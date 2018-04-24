namespace Parkmeter.Core.Models
{
    public enum VehicleTypes
    {
        Car = 0,
        Cycle = 1,
        Bike = 2,
        Truck = 3
    }

    public enum SpecialAttributes
    {
        None,
        DisabledPeople,
        SpecialNeeds,
        NotAvailable
    }

    public class Space : BaseEntity
    {
        public VehicleTypes VehicleType { get; set; }
        public SpecialAttributes SpecialAttribute { get; set; }

        public int ParkingID { get; set; }


        public static bool operator ==(Space p, Space p2)
        {
            return p.Equals(p2);
        }

        public static bool operator !=(Space p, Space p2)
        {
            return !p.Equals(p2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Space))
                return false;
            return this.ID == ((Space)obj).ID;

        }
    }
}