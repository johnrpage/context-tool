namespace ContextSketch.Core.Tests;

internal static class SampleDiagrams
{
    public const string Shop = """
    {
      "schemaVersion": 1,
      "title": "Online Shop",
      "elements": [
        { "id": "sys", "type": "system", "name": "Shop Platform", "description": "Web shop and order handling", "x": 0, "y": 0, "pinned": false },
        { "id": "cust", "type": "primaryActor", "name": "Customer", "x": -350, "y": -80, "pinned": false },
        { "id": "admin", "type": "primaryActor", "name": "Shop Admin", "x": -350, "y": 120, "pinned": false },
        { "id": "pay", "type": "secondaryActor", "name": "Payment Provider", "technology": "Stripe", "x": 350, "y": -150, "pinned": false },
        { "id": "mail", "type": "externalSystem", "name": "Email Service", "technology": "SMTP", "x": 350, "y": 0, "pinned": false },
        { "id": "db", "type": "storage", "name": "Orders DB", "technology": "PostgreSQL", "x": 350, "y": 180, "pinned": false }
      ],
      "relationships": [
        { "id": "r1", "from": "cust", "to": "sys", "label": "Places orders", "technology": "HTTPS", "direction": "oneWay" },
        { "id": "r2", "from": "admin", "to": "sys", "label": "Manages catalogue", "technology": "HTTPS", "direction": "oneWay" },
        { "id": "r3", "from": "sys", "to": "pay", "label": "Takes payment", "technology": "REST/JSON", "direction": "twoWay" },
        { "id": "r4", "from": "sys", "to": "mail", "label": "Sends receipts", "technology": "SMTP", "direction": "oneWay" },
        { "id": "r5", "from": "sys", "to": "db", "label": "Reads/writes orders", "technology": "SQL", "direction": "twoWay" }
      ]
    }
    """;
}
