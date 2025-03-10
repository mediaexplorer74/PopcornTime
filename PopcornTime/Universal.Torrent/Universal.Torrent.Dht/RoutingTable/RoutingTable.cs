#if !DISABLE_DHT
//
// RoutingTable.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Universal.Torrent.Dht.EventArgs;
using Universal.Torrent.Dht.Nodes;

namespace Universal.Torrent.Dht.RoutingTable
{
    internal class RoutingTable
    {
        public RoutingTable()
            : this(new Node(NodeId.Create(), new IPEndPoint(IPAddress.Any, 0)))
        {
        }

        public RoutingTable(Node localNode)
        {
            if (localNode == null)
                throw new ArgumentNullException(nameof(localNode));

            LocalNode = localNode;
            localNode.Seen();
            Add(new Bucket());
        }


        internal List<Bucket> Buckets { get; } = new List<Bucket>();

        public Node LocalNode { get; }

        public event EventHandler<NodeAddedEventArgs> NodeAdded;

        public bool Add(Node node)
        {
            return Add(node, true);
        }

        private bool Add(Node node, bool raiseNodeAdded)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Bucket bucket = Buckets.Find(b => b.CanContain(node));

            if (bucket.Nodes.Contains(node))
                return false;

            bool added = bucket.Add(node);
            
            if (added && raiseNodeAdded)
            {
                RaiseNodeAdded(node);
            }

            if (!added && bucket.CanContain(LocalNode))
            {
                if (Split(bucket))
                {
                    return Add(node, raiseNodeAdded);
                }
            }

            return added;
        }

        private void RaiseNodeAdded(Node node)
        {
            var h = NodeAdded;
            h?.Invoke(this, new NodeAddedEventArgs(node));
        }

        private void Add(Bucket bucket)
        {
            Buckets.Add(bucket);
            Buckets.Sort();
        }

        internal Node FindNode(NodeId id)
        {
            return Buckets.SelectMany(b => b.Nodes).FirstOrDefault(n => n.Id.Equals(id));
        }

        private void Remove(Bucket bucket)
        {
            Buckets.Remove(bucket);
        }

        private bool Split(Bucket bucket)
        {
            if (bucket.Max - bucket.Min < Bucket.MaxCapacity)
                return false; //to avoid infinit loop when add same node

            var median = (bucket.Min + bucket.Max)/2;
            var left = new Bucket(bucket.Min, median);
            var right = new Bucket(median, bucket.Max);

            Remove(bucket);
            Add(left);
            Add(right);

            foreach (var n in bucket.Nodes)
                Add(n, false);

            if (bucket.Replacement != null)
                Add(bucket.Replacement, false);

            return true;
        }

        public int CountNodes()
        {
            return Buckets.Sum(b => b.Nodes.Count);
        }


        public List<Node> GetClosest(NodeId target)
        {
            var sortedNodes = new SortedList<NodeId, Node>(Bucket.MaxCapacity);

            foreach (var b in Buckets)
            {
                foreach (var n in b.Nodes)
                {
                    var distance = n.Id.Xor(target);
                    if (sortedNodes.Count == Bucket.MaxCapacity)
                    {
                        if (distance > sortedNodes.Keys[sortedNodes.Count - 1]) //maxdistance
                            continue;
                        //remove last (with the maximum distance)
                        sortedNodes.RemoveAt(sortedNodes.Count - 1);
                    }
                    sortedNodes.Add(distance, n);
                }
            }
            return new List<Node>(sortedNodes.Values);
        }

        internal void Clear()
        {
            Buckets.Clear();
            Add(new Bucket());
        }
    }
}

#endif