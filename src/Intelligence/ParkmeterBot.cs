// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using Parkmeter.Bot;

namespace Parkmeter
{
	/// <summary>
	/// Represents a bot that processes incoming activities.
	/// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
	/// This is a Transient lifetime service. Transient lifetime services are created
	/// each time they're requested. Objects that are expensive to construct, or have a lifetime
	/// beyond a single turn, should be carefully managed.
	/// For example, the <see cref="MemoryStorage"/> object and associated
	/// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
	/// </summary>
	/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
	public class ParkmeterBot : IBot
	{
		//TODO: Step 5 -> declare variables
		private readonly ParkmeterAccessors _accessors;
		private DialogSet _dialogs;

		//TODO: LUIS(3) -> declare variables
		private readonly string LuisKey = "LuisBot";
		private readonly BotServices _services;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary> 
		//TODO: Step 6 -> edit constructor
		//passing accessors to it and editing the logic
		public ParkmeterBot(ParkmeterAccessors accessors)
		{
			_accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

			//TODO: LUIS(4) -> setup services
			_services = _services ?? throw new ArgumentNullException(nameof(_services));
			if (!_services.LuisServices.ContainsKey(LuisKey))
			{
				throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a LUIS service named '{LuisKey}'.");
			}

			// The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
			_dialogs = new DialogSet(accessors.ConversationDialogState);

			// This array defines how the Waterfall will execute.
			var waterfallSteps = new WaterfallStep[]
			{
				NameStepAsync,
				PlateStepAsync,
				ConfirmStepAsync,
				SummaryStepAsync
			};

			//TODO: LUIS(5) Waterfalls
			/* 
			var waterfallSteps = new WaterfallStep[]
            {
				ParkingNameStepAsync,
				VehicleTypeStepAsync,
                PlateStepAsync,
                SummaryStepAsync
            };
			 */

			// Add named dialogs to the DialogSet. These names are saved in the dialog state.
			_dialogs.Add(new WaterfallDialog("booking", waterfallSteps));
			_dialogs.Add(new TextPrompt("pname"));
			_dialogs.Add(new TextPrompt("plate"));
			_dialogs.Add(new ConfirmPrompt("confirm"));

			//TODO: LUIS(6) add dialogs
			/*
			_dialogs.Add(new WaterfallDialog("booking", waterfallSteps));
			_dialogs.Add(new TextPrompt("parking"));
			_dialogs.Add(new ConfirmPrompt("vehicle"));
			_dialogs.Add(new TextPrompt("plate"));
			*/
		}

		/// <summary>
		/// Every conversation turn calls this method.
		/// </summary>
		/// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
		/// for processing this conversation turn. </param>
		/// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
		/// or threads to receive notice of cancellation.</param>
		/// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
		/// <seealso cref="BotStateSet"/>
		/// <seealso cref="ConversationState"/>
		public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Handle Message activity type, which is the main activity type for shown within a conversational interface
			// Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
			// see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
			if (turnContext.Activity.Type == ActivityTypes.Message)
			{
				//TODO: Step 8 -> implement the call to the dialog
				// Run the DialogSet - let the framework identify the current state of the dialog from
				// the dialog stack and figure out what (if any) is the active dialog.
				var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
				var results = await dialogContext.ContinueDialogAsync(cancellationToken);

				// If the DialogTurnStatus is Empty we should start a new dialog.
				if (results.Status == DialogTurnStatus.Empty)
				{
					await dialogContext.BeginDialogAsync("booking", null, cancellationToken);
				}
				else if (results.Status == DialogTurnStatus.Complete)
				{
					await turnContext.SendActivityAsync("Thanks! bye bye!");
				}
			}

			//TODO: LUIS(7) -> Adding LUIS check
			/*
			if (turnContext.Activity.Type == ActivityTypes.Message)
			{
				//Check LUIS 
				var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
				var topIntent = recognizerResult?.GetTopScoringIntent();
				if (topIntent != null && topIntent.HasValue) //topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None"
				{
					var intent = topIntent.Value.intent;
					var userEntity = ParseLUISEntities(recognizerResult, intent);
					await turnContext.SendActivityAsync($"==>LUIS Top Scoring Intent: {topIntent.Value.intent}, Score: {topIntent.Value.score}, Entity: {userEntity}\n");

					// Run the DialogSet - let the framework identify the current state of the dialog from
					// the dialog stack and figure out what (if any) is the active dialog.
					var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
					var results = await dialogContext.ContinueDialogAsync(cancellationToken);

					if (intent == "Booking" || results.Status == DialogTurnStatus.Waiting)
					{
						// If the DialogTurnStatus is Empty we should start a new dialog.
						if (results.Status == DialogTurnStatus.Empty)
						{
							await dialogContext.BeginDialogAsync("booking", null, cancellationToken);
						}
						else if(results.Status == DialogTurnStatus.Complete)
						{
							await turnContext.SendActivityAsync("Thanks! bye bye!");
						}
					}
				}
				else
				{
					var msg = @"No LUIS intents were found.
                        Try typing 'Book a spot' or 'Find my car'.";
					await turnContext.SendActivityAsync(msg);
				}
            }
			*/


			// Save the dialog state into the conversation state.
			await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

			// Save the user profile updates into the user state.
			await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
		}

		//TODO: Step 7 -> defining waterfall steps
		//TODO: Step 7a -> asking parking name
		private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
			// Running a prompt here means the next WaterfallStep will be run when the users response is received.
			return await stepContext.PromptAsync("pname", new PromptOptions { Prompt = MessageFactory.Text("Please enter the parking name.") }, cancellationToken);
		}

		//TODO: Step 7b -> storing answer and asking the licence plate
		private async Task<DialogTurnResult> PlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// We can send messages to the user at any point in the WaterfallStep.
			if ((string)stepContext.Result == null)
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No parking name given."), cancellationToken);

			}
			else
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// Update the profile.
				userProfile.ParkingName = (string)stepContext.Result;

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The parking name is {userProfile.ParkingName}."), cancellationToken);

			}

			return await stepContext.PromptAsync("plate", new PromptOptions { Prompt = MessageFactory.Text("Please enter your licence plate.") }, cancellationToken);

		}

		//TODO: Step 7c -> storing answer and preparing the summary to show the user
		private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((string)stepContext.Result == null)
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No licence plate given."), cancellationToken);

			}
			else
			{
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// Update the profile.
				userProfile.LicencePlate = (string)stepContext.Result;

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The licence plate as {userProfile.LicencePlate}."), cancellationToken);
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
			return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Is this ok?") }, cancellationToken);
		}

		//TODO: Step 7d -> displaying the info
		private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((bool)stepContext.Result)
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// TODO: eseguire la chiamata a CosmosDB per ritornare i dati

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I have booked a spot in the {userProfile.ParkingName} for the car with licence plate {userProfile.LicencePlate}."), cancellationToken);

			}
			else
			{
				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your profile will not be kept."), cancellationToken);
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
			return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
		}

		//TODO: LUIS(8): Waterfall implementation
		//TODO: LUIS(8a):
		/*
		private async Task<DialogTurnResult> ParkingNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
			// Running a prompt here means the next WaterfallStep will be run when the users response is received.

			// Get the current profile object from user state.
			var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

			if (string.IsNullOrEmpty(userProfile.ParkingName))
			{
				return await stepContext.PromptAsync("parking", new PromptOptions { Prompt = MessageFactory.Text("Where do you want to book your spot? Enter parking name.") }, cancellationToken);
			}
			else
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Parking name is {userProfile.ParkingName}."), cancellationToken);
				return await stepContext.PromptAsync("parking", new PromptOptions { Prompt = MessageFactory.Text("Is the parking name correct?") }, cancellationToken);
			}
		}
		*/

		//TODO: LUIS(8b): 
		/*
		private async Task<DialogTurnResult> VehicleTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((string)stepContext.Result == null)
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No parking name understood."), cancellationToken);

			}
			else
			{
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				if (string.IsNullOrEmpty(userProfile.ParkingName))
				{
					// Update the profile.
					userProfile.ParkingName = (string)stepContext.Result;
					// We can send messages to the user at any point in the WaterfallStep.
					await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The parking name is {userProfile.ParkingName}."), cancellationToken);
				}
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
			return await stepContext.PromptAsync("vehicle", new PromptOptions { Prompt = MessageFactory.Text("Is your vehicle a car?") }, cancellationToken);
		}
		*/

		//TODO: LUIS(8c):
		/*
		private async Task<DialogTurnResult> PlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((bool)stepContext.Result)
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				userProfile.VehicleType = "car";

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"So you have a {userProfile.VehicleType}! Nice!"), cancellationToken);
				return await stepContext.PromptAsync("plate", new PromptOptions { Prompt = MessageFactory.Text("Can you please enter your licence plate?") }, cancellationToken);
			}
			else
			{
				return await stepContext.PromptAsync("plate", new PromptOptions { Prompt = MessageFactory.Text("No need for a licence plate then! Correct?") }, cancellationToken);
			}
		}
		*/

		//TODO: LUIS(8d):
		/*
		private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((string)stepContext.Result != null)
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// Update the profile.
				userProfile.Plate = (string)stepContext.Result;

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($" I've booked a spot for your {userProfile.VehicleType} with plate {userProfile.Plate} " +
					$"in the parking named {userProfile.ParkingName}"), cancellationToken);

			}
			else
			{
				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your profile will not be kept."), cancellationToken);
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
			return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
		}
		*/

		//TODO: LUIS(8e):
		/*
		private string ParseLUISEntities(RecognizerResult recognizerResult, string topIntent)
		{
			var result = string.Empty;

			if (topIntent == "Booking")
			{
				foreach (var entity in recognizerResult.Entities)
				{
					//Parse the JSON object for a known entity types: ParkingName, SpaceID, VehicleType, LicencePlate
					var pnameFound = JObject.Parse(entity.Value.ToString())["ParkingName"];
					var spaceFound = JObject.Parse(entity.Value.ToString())["SpaceID"];
					var vehicleFound = JObject.Parse(entity.Value.ToString())["VehicleType"];

					//We will return info on the entity found
					//Parking Name
					if (pnameFound != null)
					{
						var entityValue = pnameFound[0]["text"];
						var entityScore = pnameFound[0]["score"];
						var entityType = pnameFound[0]["type"];
						result = "Text: " + entityValue + "Type: " + entityType + ", Score: " + entityScore + ".";

						return result;
					}

					//SpaceID
					if (spaceFound != null)
					{
						var entityValue = spaceFound[0]["text"];
						var entityScore = spaceFound[0]["score"];
						var entityType = spaceFound[0]["type"];
						result = "Text: " + entityValue + "Type: " + entityType + ", Score: " + entityScore + ".";

						return result;
					}

					//Vehicle Type
					if (vehicleFound != null)
					{
						var entityValue = vehicleFound[0]["text"];
						var entityScore = vehicleFound[0]["score"];
						var entityType = vehicleFound[0]["type"];
						result = "Text: " + entityValue + "Type: " + entityType + ", Score: " + entityScore + ".";

						return result;
					}

				}
			}
			else if (topIntent == "Finding")
			{
				// Add bot behavior on the Finding conversation 
			}
			else
			{
				//TODO
			}

			//No entities were found, empty string is returned
			return result;
		}*/
	}

}