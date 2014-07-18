// Skeleton implementation written by Joe Zachary for CS 3500, September 2012. (Version 1.0)
// Complete implementation written by John Skyler Chase for CS3500, September 10, 2012.

using System.Linq;
using System.Collections.Generic;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// (Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.)
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///
    /// For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    ///     dependents("a") = {"b", "c"}
    ///     dependents("b") = {"d"}
    ///     dependents("c") = {}
    ///     dependents("d") = {"d"}
    ///     dependees("a") = {}
    ///     dependees("b") = {"a"}
    ///     dependees("c") = {"a"}
    ///     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        //This implementation of DependencyGraph uses a HashSet of KeyValue<string, string> Pairs as its internal structure.
        //The "Key" in the key value pair always represents the Dependee of an ordered pair and the "Value" in 
        //the key value pair always represents the Dependent of an ordered pair.
        //
        //The HashSet data structure was implemented so that it automatically handled duplicate elements and null and empty string values.
        //Also, this data structure is enumerable.  In addition, this DependencyGraph implementation uses Language Integrated Query (LINQ)
        //expressions and methods in order to retrieve specific information from the HashSet.
        private readonly HashSet<KeyValuePair<string, string>> _dependencyGraphHashSet;

        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            //initialize an empty HashSet of key values pairs to represent the empty DependencyGraph
            _dependencyGraphHashSet = new HashSet<KeyValuePair<string, string>>();
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            //simply return the number of elements in the hash set using the count property
            get { return _dependencyGraphHashSet.Count; }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        /// <param name="s">Dependent in DependencyGraph</param>
        /// <returns>Size of dependees(s)</returns>
        public int this[string s]
        {
            //use the count method of the hash set that returns the count of elements in the set
            //that satisfy the given condition.  In this case we want the count of the number of 
            //elements whose Values (Dependents) in the key value pairs equal s.
            get { return _dependencyGraphHashSet.Count(pair => pair.Value == s); }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        /// <param name="s">Dependee in DependencyGraph</param>
        /// <returns>Size of dependents(s)</returns>
        public bool HasDependents(string s)
        {
            //use the count method of the hash set that returns the count of elements in the set
            //that satisfy the given condition.  In this case we want to determine if the count of the number of 
            //elements, whose Keys (Dependees) in the key value pairs equal s, is greater than zero.
            return _dependencyGraphHashSet.Count(orderedPair => orderedPair.Key == s) > 0;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        /// <param name="s">Dependent in DependencyGraph</param>
        /// <returns>True if s has dependees, False otherwise</returns>
        public bool HasDependees(string s)
        {
            //use the count method of the hash set that returns the count of elements in the set
            //that satisfy the given condition.  In this case we want to determine if the count of the number of 
            //elements, whose Values (Dependents) in the key value pairs equal s, is greater than zero.
            return _dependencyGraphHashSet.Count(orderedPair => orderedPair.Value == s) > 0;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        /// <param name="s">Dependee in DependencyGraph</param>
        /// <returns>List of dependents(s)</returns>
        public IEnumerable<string> GetDependents(string s)
        {
            //use LINQ expression to find all elements of the HashSet whose Key (Dependee) equals s.
            return from orderPairs in _dependencyGraphHashSet where orderPairs.Key == s select orderPairs.Value;
        }


        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        /// <param name="s">Dependent in DependencyGraph</param>
        /// <returns>List of dependees(s)</returns>
        public IEnumerable<string> GetDependees(string s)
        {
            //use LINQ expression to find all elements of the HashSet whose Value (Dependent) equals s.
            return from orderPairs in _dependencyGraphHashSet where orderPairs.Value == s select orderPairs.Key;
        }


        /// <summary>
        /// Adds the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s">Dependee of t to add to DependencyGraph</param>
        /// <param name="t">Dependent of s to add to DependencyGraph</param>
        public void AddDependency(string s, string t)
        {
            //simply create a new KeyValue pair from the given s and t and add it to the HashSet
            //if this keyvalue pair already exists then the HashSet remains unchanged. 
            _dependencyGraphHashSet.Add(new KeyValuePair<string, string>(s, t));
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s">Dependee of t to remove from DependencyGraph</param>
        /// <param name="t">Dependent of s to remove from DependencyGraph</param>
        public void RemoveDependency(string s, string t)
        {
            //create a new KeyValue pair from the given s and t and use it as the parameter to the 
            //remove method of the HashSe. If this keyvalue pair doesn't exists then the HashSet remains unchanged. 
            _dependencyGraphHashSet.Remove(new KeyValuePair<string, string>(s, t));
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        /// <param name="s">Dependee in DependencyGraph</param>
        /// <param name="newDependents">Dependents of s to add to DependencyGraph</param>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            List<KeyValuePair<string, string>> toRemove = new List<KeyValuePair<string, string>>();

            //First need to make a copy of the values to remove
            foreach (KeyValuePair<string, string> orderedPair in _dependencyGraphHashSet.Where(orderedPair => orderedPair.Key == s))
                toRemove.Add(new KeyValuePair<string, string>(orderedPair.Key, orderedPair.Value));

            //First remove all the Ordered Pairs with the given Key (Dependee) from the HashSet
            foreach (KeyValuePair<string, string> orderedPair in toRemove)
                RemoveDependency(orderedPair.Key, orderedPair.Value);

            //Then add Ordered Pairs to the HashSet with the given Key (Dependee) and the new dependents
            foreach (string val in newDependents)
                AddDependency(s, val);
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        /// <param name="s">Dependent in DependencyGraph</param>
        /// <param name="newDependees">Dependees of s to add to DependencyGraph</param>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            List<KeyValuePair<string, string>> toRemove = new List<KeyValuePair<string, string>>();

            //First need to make a copy of the values to remove
            foreach (KeyValuePair<string, string> orderedPair in _dependencyGraphHashSet.Where(orderedPair => orderedPair.Value == s))
                toRemove.Add(new KeyValuePair<string, string>(orderedPair.Key, orderedPair.Value));

            //Remove all the Ordered Pairs with the given Value (Dependent) from the HashSet
            foreach (KeyValuePair<string, string> orderedPair in toRemove)
                RemoveDependency(orderedPair.Key, orderedPair.Value);

            //Then add Ordered Pairs to the HashSet with the given Value (Dependent) and the new dependees
            foreach (string key in newDependees)
                AddDependency(key, s);

        }
    }

}


