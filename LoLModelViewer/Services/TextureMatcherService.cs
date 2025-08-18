using System;
using System.Collections.Generic;
using System.Linq;

namespace LoLModelViewer.Services
{
    public class TextureMatcherService
    {
        public string? FindBestTextureMatch(string materialName, IEnumerable<string> availableTextureKeys, string? defaultTextureKey)
        {
            if (string.IsNullOrWhiteSpace(materialName) && defaultTextureKey != null)
            {
                return defaultTextureKey;
            }

            var scores = new Dictionary<string, int>();
            foreach (var key in availableTextureKeys)
            {
                scores[key] = CalculateScore(materialName, key, defaultTextureKey);
            }

            var bestMatch = scores.OrderByDescending(kv => kv.Value).FirstOrDefault();

            // If the best score is below a certain confidence threshold, it's better to use the default texture.
            if (bestMatch.Value < 50 && defaultTextureKey != null)
            {
                return defaultTextureKey;
            }
            
            return bestMatch.Key;
        }

        private int CalculateScore(string materialName, string textureKey, string? defaultTextureKey)
        {
            int score = 0;

            // Highest confidence: exact match
            if (textureKey.Equals(materialName, StringComparison.OrdinalIgnoreCase))
            {
                return 1000;
            }

            // High confidence: texture name starts with material name
            if (textureKey.StartsWith(materialName, StringComparison.OrdinalIgnoreCase))
            {
                score += 500;
            }

            // Partial word matching logic - this is the most important part for fuzzy matching
            var materialParts = materialName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (materialParts.Length > 0)
            {
                int matchedParts = 0;
                foreach (var part in materialParts)
                {
                    if (part.Length < 3) continue; // Ignore very short parts like 'v' or 'fx'
                    if (textureKey.Contains(part, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedParts++;
                    }
                }
                
                if (matchedParts > 0)
                {
                    // Award a significant score based on the ratio of matched parts.
                    // This ensures that 'tail_large' gets a high score for a texture with 'tails' in it.
                    score += (int)(200 * ((double)matchedParts / materialParts.Length));
                }
            }

            // Give the default texture a baseline score so it's a safe fallback
            if (defaultTextureKey != null && textureKey.Equals(defaultTextureKey, StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }

            return score;
        }
    }
}