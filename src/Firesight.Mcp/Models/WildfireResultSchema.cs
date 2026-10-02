using System.Text.Json;

namespace Firesight.Mcp.Models;

public static class WildfireResultSchema
{
    private const string WildfireObjectSchema =
        """
        {
          "type": "object",
          "properties": {
            "id": {
              "type": "string",
              "format": "uuid"
            },
            "externalId": {
              "type": "string"
            },
            "agency": {
              "type": "string"
            },
            "name": {
              "description": "Always null. The CWFIS feed doesn't provide fire names; null here is normal, not an error.",
              "anyOf": [
                { "type": "string" },
                { "type": "null" }
              ]
            },
            "latitude": {
              "type": "number"
            },
            "longitude": {
              "type": "number"
            },
            "startDate": {
              "description": "Always null. The CWFIS feed doesn't provide start dates; null here is normal, not an error.",
              "anyOf": [
                { "type": "string", "format": "date-time" },
                { "type": "null" }
              ]
            },
            "areaHectares": {
              "description": "Size in hectares, or null when CWFIS hasn't reported a size. Null means not available, not an error.",
              "anyOf": [
                { "type": "number" },
                { "type": "null" }
              ]
            },
            "status": {
              "description": "Stage of control exactly as CWFIS reports it: OC (out of control), BH (being held), UC (under control) or EX (extinguished).",
              "type": "string"
            },
            "statusDateUtc": {
              "description": "When CWFIS last updated the status, or null when CWFIS didn't report it. Null means not available, not an error.",
              "anyOf": [
                { "type": "string", "format": "date-time" },
                { "type": "null" }
              ]
            },
            "lastSeenInFeedUtc": {
              "description": "When this fire last appeared in the CWFIS feed.",
              "type": "string",
              "format": "date-time"
            },
            "isStale": {
              "description": "True when the fire hasn't appeared in the CWFIS feed recently (see lastSeenInFeedUtc). This only means Firesight's copy may be out of date. It says nothing about the fire's status: don't assume a stale fire is out, and use status for that.",
              "type": "boolean"
            }
          },
          "required": [
            "id",
            "externalId",
            "agency",
            "name",
            "latitude",
            "longitude",
            "startDate",
            "areaHectares",
            "status",
            "statusDateUtc",
            "lastSeenInFeedUtc",
            "isStale"
          ],
          "additionalProperties": false
        }
        """;

    public static JsonElement ActiveWildfires { get; } =
        Parse(
            $$"""
            {
              "type": "array",
              "items": {{WildfireObjectSchema}}
            }
            """);

    public static JsonElement WildfireById { get; } =
        Parse(
            $$"""
            {
              "anyOf": [
                {{WildfireObjectSchema}},
                { "type": "null" }
              ]
            }
            """);

    public static JsonElement NearbyWildfires { get; } =
        Parse(
            $$"""
            {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "wildfire": {{WildfireObjectSchema}},
                  "distanceKm": {
                    "type": "number"
                  }
                },
                "required": [
                  "wildfire",
                  "distanceKm"
                ],
                "additionalProperties": false
              }
            }
            """);

    private static JsonElement Parse(string schema) =>
        JsonDocument.Parse(schema).RootElement.Clone();
}
