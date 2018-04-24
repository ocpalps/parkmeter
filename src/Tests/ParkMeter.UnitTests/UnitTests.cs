using Parkmeter.Core.Models;
using System;
using Xunit;

namespace ParkMeter.UnitTests
{
    public class ParkingTest
    {
        [Fact]
        public void TestParking()
        {
            Parking p1 = new Parking() { ID = 1 };
            Parking p2 = new Parking() { ID = 1 };
            Assert.True(p1 == p2);
        }

        [Fact]
        public void TestSpace()
        {
            Space s1 = new Space() { ID = 1 };
            Space s2 = new Space() { ID = 1 };
            Assert.True(s1 == s2);
        }
    }
}
