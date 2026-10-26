using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
string[] values = ["  ACTOR-A  ", "\tMiXeD\r\n", "\u00A0ÉΣİK𐐀\u2003", "\u001cA\u001c", "ßẞ", "ＡＢＣ", "\u200B A \u200B"];
var rows = values.Select(input => {
    string inputJson = JsonSerializer.Serialize("party:" + input, options);
    PartyId deserialized = JsonSerializer.Deserialize<PartyId>(inputJson, options)!;
    return new { input, inputJson, constructed = new PartyId(input).Value, deserialized = deserialized.Value,
        serialized = JsonSerializer.Serialize(deserialized, options) };
});
Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions(options) { WriteIndented = true }));
