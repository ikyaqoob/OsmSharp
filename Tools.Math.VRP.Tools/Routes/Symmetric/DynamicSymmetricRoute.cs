﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tools.Math.VRP.Core.Routes.Symmetric
{
    /// <summary>
    /// An implementation for a symmetric route with varying customer counts.
    /// </summary>
    public class DynamicSymmetricRoute : ISymmetricRoute
    {
        IList<int> _customers = null;

        public DynamicSymmetricRoute(int customer)
        {
            _customers = new List<int>();
            _customers.Add(customer);
        }

        public DynamicSymmetricRoute(IList<int> customers)
        {
            _customers = new List<int>(customers);
        }

        public bool IsEmpty
        {
            get 
            { 
                return _customers.Count == 0; 
            }
        }

        public bool IsRound
        {
            get 
            { 
                return true; 
            }
        }

        public bool Contains(int from, int to)
        {
            for (int idx = 0; idx < _customers.Count - 1; idx++)
            {
                if (_customers[idx] == from && _customers[idx + 1] == to)
                {
                    return true;
                }
                else if (_customers[idx] == to && _customers[idx + 1] == from)
                {
                    return true;
                }
            }
            if (this.IsRound)
            {
                if (_customers[0] == from && _customers[_customers.Count - 1] == to)
                {
                    return true;
                }
                else if (_customers[_customers.Count - 1] == to && _customers[0] == from)
                {
                    return true;
                }
            }
            return false;
        }

        public void Insert(int from, int customer, int to)
        {
            for (int idx = 0; idx < _customers.Count - 1; idx++)
            {
                if (_customers[idx] == from && _customers[idx + 1] == to)
                {
                    _customers.Insert(idx + 1, customer);
                    return;
                }
                else if (_customers[idx] == to && _customers[idx + 1] == from)
                {
                    _customers.Insert(idx + 1, customer);
                    return;
                }
            }
            if (this.IsRound)
            {
                if (_customers[0] == from && _customers[_customers.Count - 1] == to)
                {
                    _customers.Add(customer);
                    return;
                }
                else if (_customers[_customers.Count - 1] == to && _customers[0] == from)
                {
                    _customers.Add(customer);
                    return;
                }
            }
            throw new ArgumentOutOfRangeException(string.Format("No edge found from {0} to {1} to insert customer {2}",
                from, to, customer));
        }

        public int[] GetNeigbours(int customer)
        {
            int before = -1;
            int after = -1;
            int idx = _customers.IndexOf(customer);

            if (idx > 0)
            {
                before = _customers[idx - 1];
            }
            else if(this.IsRound)
            {
                before = _customers[_customers.Count - 1];
            }
            if (idx < _customers.Count - 1)
            {
                after = _customers[idx + 1];
            }
            else if (this.IsRound)
            {
                after = _customers[0];
            }
            int[] neighbours = null;
            if (after >= 0 && before >= 0)
            {
                neighbours = new int[2];
                neighbours[0] = before;
                neighbours[1] = after;
            }
            else if (after >= 0)
            {
                neighbours = new int[1];
                neighbours[0] = after;
            }
            else if (before >= 0)
            {
                neighbours = new int[1];
                neighbours[0] = before;
            }
            return neighbours;
        }

        public bool IsValid()
        {
            return true;
        }

        public object Clone()
        {
            return new DynamicSymmetricRoute(_customers);
        }
        
        public int Count
        {
            get
            {
                return this.Count<int>();
            }
        }

        public bool Remove(int customer)
        {
            for (int idx = 0; idx < _customers.Count - 1; idx++)
            {
                if (_customers[idx] == customer)
                {
                    _customers[idx] = _customers[customer];
                    _customers[customer] = -1;
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _customers.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        public int First
        {
            get { throw new NotImplementedException(); }
        }

        public int Last
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns the index of the given customer.
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public int GetIndexOf(int customer)
        {
            int idx = 0;
            foreach (int possible_customer in this)
            {
                if (possible_customer == customer)
                {
                    return idx;
                }
                idx++;
            }
            return -1; // customer not found!
        }


        public IEnumerable<int> Between(int from, int to)
        {
            return new BetweenEnumerable(this, from, to);
        }
    }
}