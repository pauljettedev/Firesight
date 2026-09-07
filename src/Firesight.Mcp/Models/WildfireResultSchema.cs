using System.Text.Json;

namespace Firesight.Mcp.Models;

public static class WildfireResultSchema
{
    public static JsonElement ActiveWildfires { get; } = JsonDocument.Parse(
        """
        {
          "type": "array",
          "items": {
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
        }
        """).RootElement.Clone();
}
