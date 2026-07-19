using RateAiArt.DTO.LeaderBoard;

namespace RateAiArt.Services
{
    public interface ILeaderBoardService
    {
        Task<List<LeaderBoardResultDto>> GetLeaderBoard(int topItems);
        Task UpdateLeaderBoardRateAsync(int publisherId, double newScore, string artUrl);
    }
}