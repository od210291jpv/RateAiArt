using Microsoft.EntityFrameworkCore;

using RateAiArt.Data;
using RateAiArt.Data.Models;
using RateAiArt.DTO.LeaderBoard;

namespace RateAiArt.Services
{
    public class LeaderBoardService : ILeaderBoardService
    {
        private readonly ApplicationContext _dbContext;

        public LeaderBoardService(ApplicationContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<List<LeaderBoardResultDto>> GetLeaderBoard(int topItems)
        {
            var topScores = await _dbContext.PublisherLeaderBoardScores
                .OrderByDescending(s => s.LeaderBoardRate)
                .Take(topItems)
                .ToListAsync();

            var result = new List<LeaderBoardResultDto>();

            foreach (var score in topScores)
            {
                ArtPublisherModel? publisher = await _dbContext.Publishers.FindAsync(score.PublisherId);
                if (publisher != null)
                {
                    result.Add(new LeaderBoardResultDto
                    {
                        Id = score.Id,
                        Publisher = new DTO.Publisher.PublisherDto
                        {
                            Id = publisher.Id,
                            Nickname = publisher.Nickname
                        },
                        LeaderBoardRate = score.LeaderBoardRate,
                        ArtUrl = score.ArtUrl,
                        EvaluationResult = null
                    });
                }
            }

            return result;
        }

        public async Task UpdateLeaderBoardRateAsync(int publisherId, double newRate, string artUrl)
        {
            var publisher = await _dbContext.Publishers.FindAsync(publisherId);
            if (publisher == null)
            {
                throw new Exception($"Publisher with ID {publisherId} not found.");
            }

            publisher.LeaderBoardRate = newRate;

            PublisherLeaderBoardScoreModel? existingScore = await _dbContext.PublisherLeaderBoardScores
                .FirstOrDefaultAsync(s => s.PublisherId == publisherId);

            if (existingScore != null)
            {
                existingScore.LeaderBoardRate = newRate;
                existingScore.ArtUrl = artUrl;
                _dbContext.PublisherLeaderBoardScores.Update(existingScore);
            }
            else 
            {
                var newScore = new PublisherLeaderBoardScoreModel
                {
                    PublisherId = publisherId,
                    LeaderBoardRate = newRate,
                    ArtUrl = artUrl
                };
                await _dbContext.PublisherLeaderBoardScores.AddAsync(newScore);
            }
          
            await _dbContext.SaveChangesAsync();
        }
    }
}
