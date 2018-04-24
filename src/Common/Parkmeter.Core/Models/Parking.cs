using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parkmeter.Core.Models
{
    public class Parking : BaseEntity
    {
        public string Name { get; set; }
        public virtual ICollection<Space> Spaces { get; set; }
        [JsonIgnore]
        public int TotalAvailableSpaces { get { return Spaces != null ? Spaces.Count : 0; } }

        public Parking()
        {
            Spaces = new List<Space>();
        }

        public void AddSpace(Space space)
        {
            Spaces.Add(space);
            
        }
        public bool DeleteSpace(int spaceID)
        {
            var s = Spaces.SingleOrDefault(e => e.ID == spaceID);
            if (s != null)
                return Spaces.Remove(s);
            else
                return false;
        }

        public static bool operator ==(Parking p, Parking p2)
        {
            return p.Equals(p2);
        }

        public static bool operator !=(Parking p, Parking p2)
        {
            return !p.Equals(p2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Parking))
                return false;
            return this.ID == ((Parking)obj).ID;
        }

    }
}
