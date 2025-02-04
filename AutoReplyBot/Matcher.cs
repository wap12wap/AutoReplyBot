namespace AutoReplyBot;

public class Matcher
{
    private readonly List<Rule> _rules;
    private readonly int _takes;

    public Matcher(List<Rule> rules, int takes)
    {
        _rules = rules;
        _takes = takes;
    }

    public class Action
    {
        public Action(string replyContent, string? emotionType)
        {
            ReplyContent = replyContent;
            EmotionType = emotionType;
        }
        public string ReplyContent { get; set; }
        public string? EmotionType { get; set; }
    }

    public Task<Action[]> Match(string content, string userName)
    {
        if (content.Contains("I am a bot")) return Task.FromResult(Array.Empty<Action>());
        var actions = _rules
            .AsParallel()
            .Where(r => (r.Keywords.Contains("*") ||
                        (r.IgnoreCase != false && r.Keywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase))) ||
                        (r.IgnoreCase == false && r.Keywords.Any(content.Contains))) &&
                        (r.TargetAuthors.Contains(userName) || r.TargetAuthors.Contains("*")) &&
                        (r.TriggerChance == null ||
                        (r.TriggerChance != null && r.TriggerChance > Random.Shared.Next(100))))
            .Take(_takes)
            .Select(async r =>
            {
                var reply = r.Replies[Random.Shared.Next(r.Replies.Count)];
                return reply.ReplyType switch
                {
                    ReplyType.PlainText => new Action(reply.Data.Trim(), r.EmotionType),
                    ReplyType.CSharpScript => new Action(await Script.Eval(reply.Data.Trim()), r.EmotionType),
                    _ => throw new ArgumentOutOfRangeException()
                };
            });
        return Task.WhenAll(actions);
    }
}