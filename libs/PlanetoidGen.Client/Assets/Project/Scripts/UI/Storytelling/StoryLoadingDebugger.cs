using PlanetoidGen.Client.BusinessLogic.Services.Storytelling;
using PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling;
using PlanetoidGen.Client.Contracts.Services.Storytelling;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetoidGen.Client.Tests.Behaviors.Storytelling
{
    public class StoryLoadingDebugger : MonoBehaviour
    {
        private string description;
        private string[] links;
        private Vector3 defCameraPos;
        private Quaternion defCameraRot;
        private GameObject worldObject;

        [Header("Main logic")]
        public TextMeshProUGUI descriptionTMP;

        public Slider overlaySlider;
        public Material overlayMaterial;

        public Camera beforeCamera;
        public Camera afterCamera;

        public List<StorySO> stories;
        public int storyIndex;
        public List<GameObject> storyPrefabs;

        public bool showUI = true;

        [Header("Buttons and toggles")]
        public Toggle terrainAfterToggle;
        public Toggle infrastructureAfterToggle;
        public TMP_Dropdown infrastructureDropdown;
        public Button cameraResetButton;
        public Button prevStoryButton;
        public Button nextStoryButton;
        public Button uiToggle;
        public List<GameObject> uiToggleElements;

        [Header("Appearance")]
        public Material[] infrastructureWayMaterials;
        public Texture[] infrastructureWayTextures;
        public Material[] infrastructureBuildingMaterials;
        public Texture[] infrastructureBuildingTextures;

        public Canvas canvas;
        private Camera canvasCamera;

        public Color afterOutlineColor = Color.yellow;
        public Color highlightColor = Color.red;
        public Color normalColor = Color.white;

        private void Awake()
        {
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (infrastructureWayMaterials.Length != infrastructureWayTextures.Length)
            {
                throw new ArgumentException("Infrastructure Way Materials and Textures length mismatch.");
            }

            if (infrastructureBuildingMaterials.Length != infrastructureBuildingTextures.Length)
            {
                throw new ArgumentException("Infrastructure Building Materials and Textures length mismatch.");
            }
        }

        private void OnEnable()
        {
            cameraResetButton.onClick.AddListener(OnCameraReset);
            prevStoryButton.onClick.AddListener(OnLoadPrevStory);
            nextStoryButton.onClick.AddListener(OnLoadNextStory);
            uiToggle.onClick.AddListener(OnUIToggle);

            overlaySlider.onValueChanged.AddListener(OnOverlayProgressUpdate);
            infrastructureAfterToggle.onValueChanged.AddListener(OnInfrastructureAfterVisibilityUpdate);
            terrainAfterToggle.onValueChanged.AddListener(OnTerrainAfterVisibilityUpdate);
            infrastructureDropdown.onValueChanged.AddListener(OnInfrastructureDisplayKindUpdate);
        }

        private void OnDisable()
        {
            infrastructureDropdown.onValueChanged.RemoveListener(OnInfrastructureDisplayKindUpdate);
            terrainAfterToggle.onValueChanged.RemoveListener(OnTerrainAfterVisibilityUpdate);
            infrastructureAfterToggle.onValueChanged.RemoveListener(OnInfrastructureAfterVisibilityUpdate);
            overlaySlider.onValueChanged.RemoveListener(OnOverlayProgressUpdate);

            uiToggle.onClick.RemoveListener(OnUIToggle);
            nextStoryButton.onClick.RemoveListener(OnLoadNextStory);
            prevStoryButton.onClick.RemoveListener(OnLoadPrevStory);
            cameraResetButton.onClick.RemoveListener(OnCameraReset);
        }

        private void Start()
        {
            stories ??= new List<StorySO>();

            if (!stories.Any())
            {
                stories.Add(new MarkdownStoryParser().ParseStory(StoryMarkdown));
            }

            if (storyIndex < 0 || storyIndex >= stories.Count)
            {
                storyIndex = 0;
            }

            LoadStory(stories[storyIndex]);

            OnNavigationButtonsUpdate();

            showUI = !showUI;
            OnUIToggle();
            OnOverlayProgressUpdate(overlaySlider.value);
            OnInfrastructureAfterVisibilityUpdate(infrastructureAfterToggle.isOn);
            OnTerrainAfterVisibilityUpdate(terrainAfterToggle.isOn);
            OnInfrastructureDisplayKindUpdate(infrastructureDropdown.value);
        }

        private void LoadStory(StorySO story)
        {
            description = story.Text;
            links = story.Links;

            descriptionTMP.text = story.Text;

            defCameraPos = story.CameraPos;
            defCameraRot = story.CameraRot;

            OnCameraReset();

            const string WorldObjectName = "World";
            var oldWorld = GameObject.Find(WorldObjectName);
            if (oldWorld != null) Destroy(oldWorld);
            worldObject = Instantiate(storyPrefabs[storyIndex]);
            worldObject.name = WorldObjectName;
        }

        public void LoadStoryByIndex(int index)
        {
            if (index >= 0 && index <= stories.Count - 1)
            {
                storyIndex = index;
                LoadStory(stories[storyIndex]);

                OnNavigationButtonsUpdate();
            }
        }

        private void OnNavigationButtonsUpdate()
        {
            nextStoryButton.interactable = storyIndex < (stories?.Count ?? 0) - 1;
            prevStoryButton.interactable = storyIndex > 0;
        }

        private void OnLoadPrevStory()
        {
            if (storyIndex > 0)
            {
                --storyIndex;
                LoadStory(stories[storyIndex]);
            }

            OnNavigationButtonsUpdate();
        }

        private void OnLoadNextStory()
        {
            if (storyIndex < stories.Count - 1)
            {
                ++storyIndex;
                LoadStory(stories[storyIndex]);
            }

            OnNavigationButtonsUpdate();
        }

        public void OnUIToggle()
        {
            showUI = !showUI;

            uiToggleElements.ForEach(x => x.SetActive(showUI));
            var text = uiToggle.GetComponentInChildren<TextMeshProUGUI>();
            text.text = showUI ? "»" : "«";
        }

        private void OnCameraReset()
        {
            var cameraParent = beforeCamera.transform.parent;
            cameraParent.position = defCameraPos;
            cameraParent.rotation = defCameraRot;
        }

        private void OnOverlayProgressUpdate(float value)
        {
            overlayMaterial.SetFloat("_Overlay_Progress", value);
        }

        private void OnInfrastructureAfterVisibilityUpdate(bool isVisible)
        {
            int layer = 1 << LayerMask.NameToLayer("PG_Infrastructure_Before");

            if (isVisible)
            {
                afterCamera.cullingMask |= layer;
            }
            else
            {
                afterCamera.cullingMask &= ~layer;
            }
        }

        private void OnTerrainAfterVisibilityUpdate(bool isVisible)
        {
            int layer = 1 << LayerMask.NameToLayer("PG_Terrain_Before");
            var outline = worldObject.GetComponentInChildren<UI.Objects.Outline>();

            if (isVisible)
            {
                afterCamera.cullingMask |= layer;
                outline.OutlineWidth = 10.0f;
                outline.OutlineColor = afterOutlineColor;
            }
            else
            {
                afterCamera.cullingMask &= ~layer;
                outline.OutlineWidth = 0.0f;
                outline.OutlineColor = Color.clear;
            }
        }

        private const string ColorShaderVariableName = "_BaseColor";
        private const string TextureShaderVariableName = "_BaseMap";

        private void OnInfrastructureDisplayKindUpdate(int displayKind)
        {
            var infrastructureParentBefore = worldObject.transform.Find("Before");
            var infrastructureParents = Enumerable
                .Range(0, infrastructureParentBefore.childCount)
                .Select(x => infrastructureParentBefore.GetChild(x).gameObject)
                .Where(x => x.name != "Terrain")
                .ToList();

            switch (displayKind)
            {
                case 0:
                    // Hide infrastructure
                    infrastructureParents.ForEach(x => x.SetActive(false));
                    break;
                case 1:
                    // Show infrastructure
                    infrastructureParents.ForEach(x => x.SetActive(true));

                    for (int i = 0; i < infrastructureWayMaterials.Length; ++i)
                    {
                        infrastructureWayMaterials[i].SetColor(ColorShaderVariableName, normalColor);
                        infrastructureWayMaterials[i].SetTexture(TextureShaderVariableName, infrastructureWayTextures[i]);
                    }

                    for (int i = 0; i < infrastructureBuildingMaterials.Length; ++i)
                    {
                        infrastructureBuildingMaterials[i].SetColor(ColorShaderVariableName, normalColor);
                        infrastructureBuildingMaterials[i].SetTexture(TextureShaderVariableName, infrastructureBuildingTextures[i]);
                    }

                    break;
                case 2:
                    // Highlight Ways
                    infrastructureParents.ForEach(x => x.SetActive(true));

                    for (int i = 0; i < infrastructureWayMaterials.Length; ++i)
                    {
                        infrastructureWayMaterials[i].SetColor(ColorShaderVariableName, highlightColor);
                        infrastructureWayMaterials[i].SetTexture(TextureShaderVariableName, null);
                    }

                    for (int i = 0; i < infrastructureBuildingMaterials.Length; ++i)
                    {
                        infrastructureBuildingMaterials[i].SetColor(ColorShaderVariableName, normalColor);
                        infrastructureBuildingMaterials[i].SetTexture(TextureShaderVariableName, infrastructureBuildingTextures[i]);
                    }

                    break;
                case 3:
                    // Highlight All
                    infrastructureParents.ForEach(x => x.SetActive(true));

                    for (int i = 0; i < infrastructureWayMaterials.Length; ++i)
                    {
                        infrastructureWayMaterials[i].SetColor(ColorShaderVariableName, highlightColor);
                        infrastructureWayMaterials[i].SetTexture(TextureShaderVariableName, null);
                    }

                    for (int i = 0; i < infrastructureBuildingMaterials.Length; ++i)
                    {
                        infrastructureBuildingMaterials[i].SetColor(ColorShaderVariableName, highlightColor);
                        infrastructureBuildingMaterials[i].SetTexture(TextureShaderVariableName, null);
                    }

                    break;
                default:
                    break;
            }
        }

        private void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                int linkIndex = TMP_TextUtilities.FindIntersectingLink(descriptionTMP, Input.mousePosition, canvasCamera);

                if (linkIndex != -1)
                {
                    var linkInfo = descriptionTMP.textInfo.linkInfo[linkIndex];
                    var linkId = linkInfo.GetLinkID();

                    if (linkId.StartsWith(IStoryParser.AnchorIdPrefix))
                    {
                        if (int.TryParse(linkId.Substring(IStoryParser.AnchorIdPrefix.Length), out linkIndex))
                        {
                            Application.OpenURL(links[linkIndex]);
                        }
                    }
                }
            }
        }

        private const string StoryMarkdown = @"Pos,-1639.713,243.4483,757.4513
Rot,0.180241361,-0.813066959,0.329372346,0.444914758
Loc,38.3869,48.9951
# Dovhenke

This satellite image shows a 40-metre crater in Dovhenke, Ukraine, northwest of Slovyansk. ([Maxar Technologies](https://www.maxar.com/products/satellite-imagery) via AP).";
    }
}
