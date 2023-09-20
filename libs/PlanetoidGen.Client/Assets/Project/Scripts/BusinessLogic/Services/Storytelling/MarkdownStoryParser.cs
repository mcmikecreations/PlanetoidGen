using PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling;
using PlanetoidGen.Client.Contracts.Services.Storytelling;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PlanetoidGen.Client.BusinessLogic.Services.Storytelling
{
    public class MarkdownStoryParser : IStoryParser
    {
        private readonly RegexOptions _regexOptions;

        private readonly Regex _headerPattern;
        private const string HeaderPlaceholder = "Story";
        private const string HeaderSize = "28px";

        /// <remarks>
        /// Handles all cases except for [a[b]c](a(b)c).
        /// </remarks>
        private readonly Regex _linkPattern;
        private const string LinkPlaceholder = "Link";

        public MarkdownStoryParser()
        {
            _regexOptions = RegexOptions.Compiled | RegexOptions.Multiline;

            _headerPattern = new Regex(@"(?:^#[\t ]+)(.*)", _regexOptions);
            _linkPattern = new Regex(@"(?<!!)(?:\[)([^\[\]]*)(?:\]\()([^\(\)]*)(?:\))", _regexOptions);
        }

        public StorySO ParseStory(string rawText)
        {
            var story = ScriptableObject.CreateInstance<StorySO>();

            rawText = rawText.Trim();

            ParseCoordinates(story, ref rawText);
            ParseLinks(story, ref rawText);
            ParseHeader(story, ref rawText);

            story.Text = rawText.Trim();

            //string path = "Assets/Project/Streamed/Storytelling/Story.asset";
            //AssetDatabase.CreateAsset(story, path);
            //AssetDatabase.SaveAssets();
            //AssetDatabase.Refresh();
            //EditorUtility.FocusProjectWindow();

            //Selection.activeObject = story;

            return story;
        }

        private void ParseCoordinates(StorySO story, ref string rawText)
        {
            var rows = rawText.Split('\n');
            var transformRows = rows.Take(3);
            var numberStyle = NumberStyles.Float;
            var numberCulture = CultureInfo.InvariantCulture.NumberFormat;
            int skipRows = 0;

            var cameraPosRow = transformRows.FirstOrDefault(x => x.StartsWith("Pos,"));
            if (cameraPosRow != null)
            {
                var cameraCoords = cameraPosRow.Trim().Split(',');
                if (cameraCoords.Length != 4)
                {
                    Debug.Log($"Wrong row or broken data: {cameraPosRow}.");
                }

                if (float.TryParse(cameraCoords[1], numberStyle, numberCulture, out float x) &&
                    float.TryParse(cameraCoords[2], numberStyle, numberCulture, out float y) &&
                    float.TryParse(cameraCoords[3], numberStyle, numberCulture, out float z))
                {
                    story.CameraPos = new Vector3(x, y, z);
                    ++skipRows;
                }
            }

            var cameraRotRow = transformRows.FirstOrDefault(x => x.StartsWith("Rot,"));
            if (cameraRotRow != null)
            {
                var cameraCoords = cameraRotRow.Trim().Split(',');
                if (cameraCoords.Length != 5)
                {
                    Debug.Log($"Wrong row or broken data: {cameraRotRow}.");
                }

                if (float.TryParse(cameraCoords[1], numberStyle, numberCulture, out float x) &&
                    float.TryParse(cameraCoords[2], numberStyle, numberCulture, out float y) &&
                    float.TryParse(cameraCoords[3], numberStyle, numberCulture, out float z) &&
                    float.TryParse(cameraCoords[4], numberStyle, numberCulture, out float w))
                {
                    story.CameraRot = new Quaternion(x, y, z, w);
                    ++skipRows;
                }
            }

            var locationRow = transformRows.FirstOrDefault(x => x.StartsWith("Loc,"));
            if (locationRow != null)
            {
                var locationCoords = locationRow.Trim().Split(',');
                if (locationCoords.Length != 3)
                {
                    Debug.Log($"Wrong row or broken data: {cameraRotRow}.");
                }

                if (double.TryParse(locationCoords[1], numberStyle, numberCulture, out double lon) &&
                    double.TryParse(locationCoords[2], numberStyle, numberCulture, out double lat))
                {
                    story.LocationLongitude = lon;
                    story.LocationLatitude = lat;
                    ++skipRows;
                }
            }

            rawText = string.Join('\n', rows.Skip(skipRows));
        }

        private void ParseHeader(StorySO story, ref string rawText)
        {
            var headers = new List<string>();

            rawText = _headerPattern.Replace(rawText, match =>
            {
                headers.Add((match.Groups[1].Value ?? HeaderPlaceholder).Trim());

                return $"<size={HeaderSize}><b>{headers[headers.Count - 1]}</b></size>";
            });
        }

        private void ParseLinks(StorySO story, ref string rawText)
        {
            var links = new List<string>();

            rawText = _linkPattern.Replace(rawText, match =>
            {
                links.Add((match.Groups[2].Value ?? string.Empty).Trim());

                return $@"<color=""blue""><link=""a_{links.Count - 1}"">{(match.Groups[1].Value ?? LinkPlaceholder).Trim()}</link></color>";
            });

            story.Links = links.ToArray();
        }
    }
}
