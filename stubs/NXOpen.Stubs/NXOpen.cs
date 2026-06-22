// SPDX-License-Identifier: GPL-2.0-only
using System;

namespace NXOpen
{
    /// <summary>Stub of NXOpen.PartUnits (real values: 0 = Millimeters, 1 = Inches).</summary>
    public enum PartUnits
    {
        Millimeters = 0,
        Inches = 1,
    }

    /// <summary>Stub base class for NX objects.</summary>
    public abstract class NXObject
    {
        public virtual string Name => throw StubError();

        protected static InvalidOperationException StubError() =>
            new InvalidOperationException(
                "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
    }

    /// <summary>Stub of NXOpen.Part.</summary>
    public class Part : NXObject
    {
        /// <summary>Leaf file name of the part (without directory or extension).</summary>
        public virtual string Leaf => throw StubError();

        /// <summary>Full path of the part's .prt file on disk.</summary>
        public virtual string FullPath => throw StubError();

        public virtual PartUnits PartUnits => throw StubError();

        /// <summary>The part's expressions (parameters + feature-internal values).</summary>
        public virtual ExpressionCollection Expressions => throw StubError();

        /// <summary>The part's solid/sheet bodies.</summary>
        public virtual BodyCollection Bodies => throw StubError();

        /// <summary>The part's feature history.</summary>
        public virtual Features.FeatureCollection Features => throw StubError();
    }

    /// <summary>Stub of NXOpen.ListingWindow (the NX text output window).</summary>
    public class ListingWindow
    {
        public virtual void Open() => throw new InvalidOperationException(StubMessage);

        public virtual void WriteLine(string line) => throw new InvalidOperationException(StubMessage);

        private const string StubMessage =
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll";
    }

    /// <summary>Stub of NXOpen.PartCollection.</summary>
    public class PartCollection
    {
        /// <summary>The current work part, or null when no part is open.</summary>
        public virtual Part? Work => throw NXObject_StubError();

        private static InvalidOperationException NXObject_StubError() =>
            new InvalidOperationException(
                "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll");
    }

    /// <summary>Stub of NXOpen.Session.</summary>
    public class Session
    {
        public virtual PartCollection Parts => throw new InvalidOperationException(StubMessage);

        public virtual ListingWindow ListingWindow => throw new InvalidOperationException(StubMessage);

        public static Session GetSession() => throw new InvalidOperationException(StubMessage);

        private const string StubMessage =
            "NXOpen stub member invoked: this assembly is compile-only; run inside NX with the real NXOpen.dll";
    }
}
