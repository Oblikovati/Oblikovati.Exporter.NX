// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Oblikovati.Exporter.NX.Recipe
{
    /// <summary>
    /// One history feature. Mirrors FeatureData in Oblikovati/model/feature/serialize.go:
    /// a <c>kind</c> discriminator plus exactly one typed payload (here <c>extrude</c>).
    /// Order in the part's features list is the feature history order.
    /// </summary>
    public sealed class FeatureData
    {
        [YamlMember(Alias = "kind")]
        public string Kind { get; set; } = string.Empty;

        [YamlMember(Alias = "name", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Name { get; set; }

        [YamlMember(Alias = "extrude", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public ExtrudeData? Extrude { get; set; }

        [YamlMember(Alias = "revolve", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public RevolveData? Revolve { get; set; }
    }

    /// <summary>
    /// An extrude payload. Mirrors ExtrudeData in serialize_extrude.go. The sketch is the
    /// array index into the part's sketches; profiles are detected region indices.
    /// Distances are centimetre database units.
    /// </summary>
    public sealed class ExtrudeData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profiles", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public IList<int> Profiles { get; } = new List<int>();

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";

        [YamlMember(Alias = "extent", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Extent { get; set; }

        [YamlMember(Alias = "direction", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Direction { get; set; }

        [YamlMember(Alias = "distance", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Distance { get; set; }

        [YamlMember(Alias = "distance2", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Distance2 { get; set; }

        [YamlMember(Alias = "taper", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Taper { get; set; }
    }

    /// <summary>
    /// A revolve payload. Mirrors RevolveData in serialize_work.go. Empty axis fields mean
    /// "revolve about the profile sketch's own centerline". Angle is in radians; 0 means a
    /// full revolution.
    /// </summary>
    public sealed class RevolveData
    {
        [YamlMember(Alias = "sketch")]
        public int Sketch { get; set; }

        [YamlMember(Alias = "profile")]
        public int Profile { get; set; }

        [YamlMember(Alias = "angle", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public double? Angle { get; set; }

        [YamlMember(Alias = "operation")]
        public string Operation { get; set; } = "newBody";
    }
}
