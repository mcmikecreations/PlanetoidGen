using System;
using UnityEngine;

namespace PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling
{
    [Serializable]
    [CreateAssetMenu(fileName = "Story", menuName = "Storytelling/Story", order = 1)]
    public class StorySO : ScriptableObject
    {
        [SerializeField]
        [Multiline]
        private string _text;
        [SerializeField]
        private string[] _links;
        [SerializeField]
        private Vector3 _cameraPos;
        [SerializeField]
        private Quaternion _cameraRot;
        [SerializeField]
        private double _locationLongitude;
        [SerializeField]
        private double _locationLatitude;
        [SerializeField]
        private int _locationZoom;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public string[] Links
        {
            get => _links;
            set => _links = value;
        }

        public Vector3 CameraPos
        {
            get => _cameraPos;
            set => _cameraPos = value;
        }

        public Quaternion CameraRot
        {
            get => _cameraRot;
            set => _cameraRot = value;
        }

        public double LocationLongitude
        {
            get => _locationLongitude;
            set => _locationLongitude = value;
        }

        public double LocationLatitude
        {
            get => _locationLatitude;
            set => _locationLatitude = value;
        }

        public int LocationZoom
        {
            get => _locationZoom;
            set => _locationZoom = value;
        }
    }
}
