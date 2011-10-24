﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Neo4jClient.Gremlin;

namespace Neo4jClient.Test.Gremlin
{
    [TestFixture]
    public class GremlinPagedEnumeratorTests
    {
        [Test]
        public void ShouldNotLoadAnythingUntilEnumerated()
        {
            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<object>> loadCallback =
                q => { loadedQueries.Add(q); return new object[0]; };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });
            
            new GremlinPagedEnumerator<object>(loadCallback, baseQuery);

            Assert.AreEqual(0, loadedQueries.Count());
        }

        [Test]
        public void ShouldLoadFirstPageOfResultsWithFirstEnumeration()
        {
            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<object>> loadCallback =
                q => { loadedQueries.Add(q); return new object[0]; };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });

            var enumerator = new GremlinPagedEnumerator<object>(loadCallback, baseQuery);
            enumerator.MoveNext();

            Assert.AreEqual(1, loadedQueries.Count());
            Assert.AreEqual("g.v(p0).outV.drop(p1).take(p2)", loadedQueries[0].QueryText);
            Assert.AreEqual(0, loadedQueries[0].QueryParameters["p1"]);
            Assert.AreEqual(100, loadedQueries[0].QueryParameters["p2"]);
        }

        [Test]
        public void ShouldEnumerateOverFirstPageOfResults()
        {
            var results = Enumerable.Range(0, 100).ToArray();

            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<int>> loadCallback =
                q => { loadedQueries.Add(q); return results; };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });

            var enumerator = new GremlinPagedEnumerator<int>(loadCallback, baseQuery);
            for (var i = 0; i < 100; i++)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(results[i], enumerator.Current);
            }
        }

        [Test]
        public void MoveNextShouldReturnFalseOnFirstCallIfThereAreNoResults()
        {
            var results = new int[0];

            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<int>> loadCallback =
                q => { loadedQueries.Add(q); return results; };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });

            var enumerator = new GremlinPagedEnumerator<int>(loadCallback, baseQuery);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void ShouldLoadSecondPageWhenCallingMoveNextAfterLastRecordOfFirstPage()
        {
            var results = Enumerable.Range(0, 100).ToArray();

            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<int>> loadCallback =
                q => { loadedQueries.Add(q); return results; };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });

            var enumerator = new GremlinPagedEnumerator<int>(loadCallback, baseQuery);
            for (var i = 0; i < 100; i++)
            {
                enumerator.MoveNext();
            }

            enumerator.MoveNext();

            Assert.AreEqual(2, loadedQueries.Count());
            Assert.AreEqual("g.v(p0).outV.drop(p1).take(p2)", loadedQueries[0].QueryText);
            Assert.AreEqual(0, loadedQueries[0].QueryParameters["p1"]);
            Assert.AreEqual(100, loadedQueries[0].QueryParameters["p2"]);
            Assert.AreEqual("g.v(p0).outV.drop(p1).take(p2)", loadedQueries[1].QueryText);
            Assert.AreEqual(100, loadedQueries[1].QueryParameters["p1"]);
            Assert.AreEqual(100, loadedQueries[1].QueryParameters["p2"]);
        }

        [Test]
        public void ShouldEnumerateOverTwoPagesOfResults()
        {
            var pages = new Queue<IEnumerable<int>>(new[]
            {
                Enumerable.Range(0, 100),
                Enumerable.Range(100, 100)
            });

            var loadedQueries = new List<IGremlinQuery>();
            Func<IGremlinQuery, IEnumerable<int>> loadCallback =
                q => { loadedQueries.Add(q); return pages.Dequeue(); };

            var baseQuery = new GremlinQuery(
                null,
                "g.v(p0).outV",
                new Dictionary<string, object> { { "p0", 0 } });

            var enumerator = new GremlinPagedEnumerator<int>(loadCallback, baseQuery);
            for (var i = 0; i < 200; i++)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(i, enumerator.Current);
            }
        }
    }
}
