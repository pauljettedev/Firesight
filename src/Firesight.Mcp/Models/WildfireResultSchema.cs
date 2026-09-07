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
              "anyOf": [
                { "type": "string", "format": "date-time" },
                { "type": "null" }
              ]
            },
            "areaHectares": {
              "anyOf": [
                { "type": "number" },
                { "type": "null" }
              ]
            },
            "status": {
              "type": "string"
            },
            "statusDateUtc": {
              "anyOf": [
                { "type": "string", "format": "date-time" },
                { "type": "null" }
              ]
            },
            "lastSeenInFeedUtc": {
              "type": "string",
              "format": "date-time"
            },
            "isStale": {
              "type": "boolean"
            }
          },
          "required": [
            "id",
            "externalId",
            "agency",
            "latitude",
            "longitude",
            "status",
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

    private static JsonElement Parse(string schema) =>
        JsonDocument.Parse(schema).RootElement.Clone();
}
