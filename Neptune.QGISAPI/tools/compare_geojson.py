#!/usr/bin/env python3
"""
Parity comparison for QGIS overlay outputs (NPT-1105 Part 2).

Compares two TGU or LGU output GeoJSON files (e.g. QGIS 3.28 golden baseline vs a
reworked/ported run on identical inputs) in an order- and fid-insensitive way:

  - feature counts and total area
  - area grouped by the layer's attribute tuple (the business identity of each piece)
  - groups present in only one output
  - per-group area deltas above a tolerance (default 0.5 m^2 -- EPSG:2771 is metric)

Pure standard library on purpose: areas come from the planar shoelace formula, which
is exact for the projected CRS (EPSG:2771) both engines write. No GDAL/shapely needed,
so the tool runs identically on a dev host, in the QGIS container, or against NTS output.

Usage:
  python3 compare_geojson.py old.geojson new.geojson --type tgu
  python3 compare_geojson.py old.geojson new.geojson --type lgu --tolerance 0.25
  python3 compare_geojson.py old.geojson new.geojson --keys DelinID,WQMPID --top 50

Exit code 0 = outputs match within tolerance; 1 = differences found; 2 = usage/input error.
"""

import argparse
import json
import sys
from collections import defaultdict

GROUP_KEYS = {
    "tgu": ["DelinID", "OVTAID", "WQMPID", "LUBID", "SJID"],
    "lgu": ["DelinID", "WQMPID", "ModelID", "RSBID"],
}


def ring_area(coords):
    """Signed shoelace area of a linear ring (positive = counterclockwise)."""
    area = 0.0
    n = len(coords)
    for i in range(n - 1):
        x1, y1 = coords[i][0], coords[i][1]
        x2, y2 = coords[i + 1][0], coords[i + 1][1]
        area += x1 * y2 - x2 * y1
    return area / 2.0


def polygon_area(rings):
    """Area of a GeoJSON Polygon: |exterior| minus |holes|."""
    if not rings:
        return 0.0
    area = abs(ring_area(rings[0]))
    for hole in rings[1:]:
        area -= abs(ring_area(hole))
    return area


def geometry_area(geometry):
    """Planar area of any GeoJSON geometry; non-areal types contribute 0."""
    if geometry is None:
        return 0.0
    geom_type = geometry.get("type")
    if geom_type == "Polygon":
        return polygon_area(geometry.get("coordinates") or [])
    if geom_type == "MultiPolygon":
        return sum(polygon_area(rings) for rings in (geometry.get("coordinates") or []))
    if geom_type == "GeometryCollection":
        return sum(geometry_area(g) for g in (geometry.get("geometries") or []))
    return 0.0  # points/lines have no area; the scripts' cleanup should have removed them


def normalize_key_value(value):
    """Make attribute values hashable and stable across serializers (1 vs 1.0 vs '1')."""
    if value is None:
        return None
    if isinstance(value, float) and value.is_integer():
        return int(value)
    if isinstance(value, str):
        stripped = value.strip()
        if stripped.lstrip("-").isdigit():
            return int(stripped)
        return stripped
    return value


def load_groups(path, keys):
    """Read a GeoJSON FeatureCollection into {group_tuple: {'area': float, 'count': int}}."""
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    features = data.get("features")
    if features is None:
        raise ValueError(f"{path}: not a GeoJSON FeatureCollection (no 'features' member)")

    groups = defaultdict(lambda: {"area": 0.0, "count": 0})
    total_area = 0.0
    null_geometry_count = 0

    for feature in features:
        properties = feature.get("properties") or {}
        group = tuple(normalize_key_value(properties.get(k)) for k in keys)
        area = geometry_area(feature.get("geometry"))
        if feature.get("geometry") is None:
            null_geometry_count += 1
        groups[group]["area"] += area
        groups[group]["count"] += 1
        total_area += area

    return {
        "groups": dict(groups),
        "feature_count": len(features),
        "total_area": total_area,
        "null_geometry_count": null_geometry_count,
    }


def format_group(keys, group):
    return "(" + ", ".join(f"{k}={v}" for k, v in zip(keys, group)) + ")"


def main():
    parser = argparse.ArgumentParser(description="Compare two TGU/LGU overlay GeoJSON outputs.")
    parser.add_argument("old_path", help="baseline output (e.g. QGIS 3.28 golden run)")
    parser.add_argument("new_path", help="candidate output (e.g. NTS port run)")
    parser.add_argument("--type", choices=sorted(GROUP_KEYS), help="layer type: picks the attribute tuple")
    parser.add_argument("--keys", help="comma-separated attribute names (overrides --type)")
    parser.add_argument("--tolerance", type=float, default=0.5,
                        help="per-group area delta to flag, in CRS units^2 (default 0.5 m^2)")
    parser.add_argument("--top", type=int, default=25, help="max rows to print per difference table")
    args = parser.parse_args()

    if args.keys:
        keys = [k.strip() for k in args.keys.split(",") if k.strip()]
    elif args.type:
        keys = GROUP_KEYS[args.type]
    else:
        parser.error("one of --type or --keys is required")

    try:
        old = load_groups(args.old_path, keys)
        new = load_groups(args.new_path, keys)
    except (OSError, ValueError, json.JSONDecodeError) as e:
        print(f"ERROR: {e}", file=sys.stderr)
        return 2

    print(f"Group keys: {', '.join(keys)}")
    print(f"{'':17}{'old':>18}{'new':>18}")
    print(f"{'features':17}{old['feature_count']:>18,}{new['feature_count']:>18,}")
    print(f"{'groups':17}{len(old['groups']):>18,}{len(new['groups']):>18,}")
    print(f"{'total area m^2':17}{old['total_area']:>18,.1f}{new['total_area']:>18,.1f}")
    if old["null_geometry_count"] or new["null_geometry_count"]:
        print(f"{'null geometries':17}{old['null_geometry_count']:>18,}{new['null_geometry_count']:>18,}")

    only_old = sorted(
        ((g, v["area"]) for g, v in old["groups"].items() if g not in new["groups"]),
        key=lambda x: -x[1])
    only_new = sorted(
        ((g, v["area"]) for g, v in new["groups"].items() if g not in old["groups"]),
        key=lambda x: -x[1])
    area_diffs = sorted(
        ((g, old["groups"][g]["area"], new["groups"][g]["area"])
         for g in old["groups"].keys() & new["groups"].keys()
         if abs(old["groups"][g]["area"] - new["groups"][g]["area"]) > args.tolerance),
        key=lambda x: -abs(x[1] - x[2]))

    def print_table(title, rows, render):
        print(f"\n{title}: {len(rows)}")
        for row in rows[:args.top]:
            print("  " + render(row))
        if len(rows) > args.top:
            print(f"  ... and {len(rows) - args.top} more")

    print_table("Groups only in OLD", only_old,
                lambda r: f"{format_group(keys, r[0])} area={r[1]:,.1f}")
    print_table("Groups only in NEW", only_new,
                lambda r: f"{format_group(keys, r[0])} area={r[1]:,.1f}")
    print_table(f"Groups with area delta > {args.tolerance} m^2", area_diffs,
                lambda r: f"{format_group(keys, r[0])} old={r[1]:,.1f} new={r[2]:,.1f} delta={r[2] - r[1]:+,.1f}")

    clean = not (only_old or only_new or area_diffs)
    print(f"\nRESULT: {'MATCH within tolerance' if clean else 'DIFFERENCES FOUND'}")
    return 0 if clean else 1


if __name__ == "__main__":
    sys.exit(main())
