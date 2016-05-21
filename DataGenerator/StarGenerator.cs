using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaxyService.Shared.Models;

namespace DataGenerator
{
    public class StarGenerator
    {
        private readonly int _minStarsPerGalaxy;
        private readonly int _maxStarsPerGalaxy;
        readonly Random _random = new Random();

        public StarGenerator(int minStarsPerGalaxy, int maxStarsPerGalaxy)
        {
            _minStarsPerGalaxy = minStarsPerGalaxy;
            _maxStarsPerGalaxy = maxStarsPerGalaxy;
        }

        public IEnumerable<StarEntity> GenerateRandomStars(int numberOfGalaxies)
        {
            var galaxies = GenerateRandomGalaxies(numberOfGalaxies);
            foreach (var galaxy in galaxies)
            {
                var numberOfStars = _random.Next(_minStarsPerGalaxy, _maxStarsPerGalaxy);
                for (var i = 0; i < numberOfStars; i++)
                {
                    yield return new StarEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        GalaxyId = galaxy.Id,
                        GalaxyName = galaxy.Name,
                        StarName = $"S_{i + 1}",
                        Data = GenerateRandomProperties()
                    };
                }
            }
        }

        private static IEnumerable<Program.Galaxy> GenerateRandomGalaxies(int number)
        {
            for (var i = 0; i < number; i++)
            {
                yield return new Program.Galaxy
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"G_{i + 1}"
                };
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GenerateRandomProperties()
        {
            var number = _random.Next(5, 21);
            for (var i = 0; i < number; i++)
            {
                yield return new KeyValuePair<string, string>($"P{i + 1}", $"V{i + 1}");
            }
        }
    }
}
