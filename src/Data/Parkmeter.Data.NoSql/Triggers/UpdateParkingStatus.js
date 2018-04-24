function UpdateParkingStatus() {
    
      // HTTP error codes sent to our callback funciton by DocDB server.
      var ErrorCode = {
          RETRY_WITH: 449,
      }
  
      var collection = getContext().getCollection();
      var collectionLink = collection.getSelfLink();
  
      // Get the document from request (the script runs as trigger, thus the input comes in request).
      var doc = getContext().getRequest().getBody();
  
      // As this trigger can insert a new document, may start an infinite loop. 
      // to avoid and do not run it when not needed, I use a property to check if must be jumped
  
      //if isStatus is true I do not process it
      if (!doc.isStatus) {
          getAndUpdateMetadata();
      }
  
      function getAndUpdateMetadata() {
          
          //query the collection to get the document with the summary
          var isAccepted = collection.queryDocuments(collectionLink, "SELECT * FROM VehicleAccesses r WHERE r.id = '_status_" + doc.ParkingID + "'", function (err, feed, options) {
  
              if (err) throw err;
  
          //I create a new document if not found
              var metaDoc;
              if (!feed || !feed.length) {
                  // Create the meta doc for this partition, using the partition key value from the request document
                  metaDoc = {
                      //here my custom document properties
                      "id": "_status_" + doc.ParkingID,
                      "parkingID": doc.ParkingID,
                      "isStatus": true,
                      "busySpaces": 0
                  }
                  metaDoc.partitionKey = doc.partitionKey;
              }
              else {
                  // Found the metadata document for this partition. So just use it
                  metaDoc = feed[0];
              }
  
              metaDoc.busySpaces += doc.Direction;
  
              // Update/replace the summary document in the store.
              var isAccepted;
              if (!feed || !feed.length) {
                  // Create the metadata document if it doesn't exist
                  isAccepted = collection.createDocument(collectionLink, metaDoc, function (err) {
  
                      if (err) throw err;
  
                      // Note: in case concurrent updates causes conflict with ErrorCode.RETRY_WITH, we can't read the meta again 
  
                      //       and update again because due to Snapshot isolation we will read same exact version (we are in same transaction).
  
                      //       We have to take care of that on the client side.
  
                  });
              }
              else {
                  // Replace the metadata document
                  isAccepted = collection.replaceDocument(metaDoc._self, metaDoc, function (err) {
  
                      if (err) throw err;
                  });
              }
  
              if (!isAccepted) throw new Error("The call replaceDocument(metaDoc) returned false.");
          });
  
          if (!isAccepted) throw new Error("The call queryDocuments for metaDoc returned false.");
  
      }
  
  }
  
  