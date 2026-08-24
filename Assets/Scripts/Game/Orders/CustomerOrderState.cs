using System;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    public enum PickResult { Correct, Wrong, Ignored }

    // One customer's order: required types with remaining counts plus the private countdown.
    public class CustomerOrderState
    {
        public List<CandyTypeDefinition> types = new List<CandyTypeDefinition>();
        public List<int> remaining = new List<int>();
        public int totalCandies;
        public float totalTime;
        public float timeLeft;
        public int wrongPicksThisCustomer;
        public bool perfect => wrongPicksThisCustomer == 0;
        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < remaining.Count; i++)
                    if (remaining[i] > 0) return false;
                return true;
            }
        }

        public int RemainingOf(string typeId)
        {
            for (int i = 0; i < types.Count; i++)
                if (types[i].typeId == typeId) return remaining[i];
            return -1; // not requested
        }
    }
}
