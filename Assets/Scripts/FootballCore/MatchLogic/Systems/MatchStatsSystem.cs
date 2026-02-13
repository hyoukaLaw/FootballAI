using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;

namespace FootballAI.FootballCore
{
public class MatchStatsSystem
{
    private readonly UnityEvent<int, int> _onScoreChanged;

    public MatchStatsSystem(UnityEvent<int, int> onScoreChanged)
    {
        _onScoreChanged = onScoreChanged;
    }

    public void InitScoreEvent(int redScore, int blueScore)
    {
        _onScoreChanged?.Invoke(redScore, blueScore);
    }

    public void AddGoal(string scoringTeam, ref int redScore, ref int blueScore, List<string> scoreChanges)
    {
        if (scoringTeam == "Red")
            redScore++;
        else if (scoringTeam == "Blue")
            blueScore++;
        scoreChanges.Add($"{redScore}:{blueScore}");
    }

    public MatchResult BuildMatchResultAndTrack(ref int currentMatchNumber, int redScore, int blueScore, List<string> scoreChanges, List<MatchResult> matchHistory)
    {
        currentMatchNumber++;
        MatchResult result = new MatchResult
        {
            MatchNumber = currentMatchNumber,
            RedFinalScore = redScore,
            BlueFinalScore = blueScore,
            ScoreChanges = new List<string>(scoreChanges)
        };
        matchHistory.Add(result);
        return result;
    }

    public void ResetForNewMatch(ref int redScore, ref int blueScore, List<string> scoreChanges)
    {
        redScore = 0;
        blueScore = 0;
        scoreChanges.Clear();
    }

    public void UpdateScoreUI(int redScore, int blueScore)
    {
        _onScoreChanged?.Invoke(redScore, blueScore);
    }

    public string BuildOneMatchResultLog(MatchResult result)
    {
        string log = $"第{result.MatchNumber}场比赛: Red {result.RedFinalScore} - Blue {result.BlueFinalScore}";
        for (int i = 0; i < result.ScoreChanges.Count; i++)
            log += $"\n{result.ScoreChanges[i]}";
        return log;
    }

    public string BuildMatchStatisticsReport(List<MatchResult> matchHistory)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("       比赛统计报告");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine("【每场比赛比分】");
        for (int i = 0; i < matchHistory.Count; i++)
        {
            MatchResult result = matchHistory[i];
            sb.AppendLine($"第{result.MatchNumber}场: 红方 {result.RedFinalScore} - 蓝方 {result.BlueFinalScore}");
        }
        sb.AppendLine();
        float redTotal = 0f;
        float blueTotal = 0f;
        int redWins = 0;
        int blueWins = 0;
        int draws = 0;
        for (int i = 0; i < matchHistory.Count; i++)
        {
            MatchResult result = matchHistory[i];
            redTotal += result.RedFinalScore;
            blueTotal += result.BlueFinalScore;
            if (result.RedFinalScore > result.BlueFinalScore)
                redWins++;
            else if (result.BlueFinalScore > result.RedFinalScore)
                blueWins++;
            else
                draws++;
        }
        float redAverage = redTotal / matchHistory.Count;
        float blueAverage = blueTotal / matchHistory.Count;
        sb.AppendLine("【统计汇总】");
        sb.AppendLine($"比赛场次: {matchHistory.Count}");
        sb.AppendLine();
        sb.AppendLine($"红方平均得分: {redAverage:F2}");
        sb.AppendLine($"蓝方平均得分: {blueAverage:F2}");
        sb.AppendLine($"红方总进球数: {redTotal}");
        sb.AppendLine($"蓝方总进球数: {blueTotal}");
        sb.AppendLine();
        sb.AppendLine($"红方胜场: {redWins}");
        sb.AppendLine($"蓝方胜场: {blueWins}");
        sb.AppendLine($"平局数: {draws}");
        sb.AppendLine();
        sb.AppendLine("【胜负分析】");
        if (redWins > blueWins)
            sb.AppendLine($"红方表现更优，领先 {redWins - blueWins} 场");
        else if (blueWins > redWins)
            sb.AppendLine($"蓝方表现更优，领先 {blueWins - redWins} 场");
        else
            sb.AppendLine("双方平分秋色");
        sb.AppendLine("========================================");
        return sb.ToString();
    }
}
}
