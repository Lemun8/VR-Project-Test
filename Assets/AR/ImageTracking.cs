using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Spawns and manages a 3D prefab for each tracked reference image.
/// Attach this to the XR Origin GameObject alongside ARTrackedImageManager.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTracking : MonoBehaviour
{
    [Tooltip("Prefabs to spawn per reference image. Index must match the image library order.")]
    [SerializeField] private GameObject[] _prefabsToSpawn;

    private ARTrackedImageManager _trackedImageManager;
    private readonly Dictionary<string, GameObject> _spawnedObjects = new();

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
            HandleAddedImage(trackedImage);

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
            HandleUpdatedImage(trackedImage);

        foreach (ARTrackedImage trackedImage in eventArgs.removed)
            HandleRemovedImage(trackedImage);
    }

    private void HandleAddedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        GameObject prefab = GetPrefabForImage(imageName);

        if (prefab == null)
        {
            Debug.LogWarning($"[ImageTracking] No prefab assigned for image: {imageName}");
            return;
        }

        GameObject spawnedObject = Instantiate(prefab, trackedImage.transform.position, trackedImage.transform.rotation);
        _spawnedObjects[imageName] = spawnedObject;
    }

    private void HandleUpdatedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (!_spawnedObjects.TryGetValue(imageName, out GameObject spawnedObject))
            return;

        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
        spawnedObject.SetActive(isTracking);

        if (isTracking)
        {
            spawnedObject.transform.SetPositionAndRotation(
                trackedImage.transform.position,
                trackedImage.transform.rotation
            );
        }
    }

    private void HandleRemovedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (_spawnedObjects.TryGetValue(imageName, out GameObject spawnedObject))
        {
            Destroy(spawnedObject);
            _spawnedObjects.Remove(imageName);
        }
    }

    /// <summary>
    /// Maps an image name to a prefab by matching the reference image library order.
    /// Override this if you prefer a name-based dictionary approach instead.
    /// </summary>
    private GameObject GetPrefabForImage(string imageName)
    {
        for (int i = 0; i < _trackedImageManager.referenceLibrary.count; i++)
        {
            if (_trackedImageManager.referenceLibrary[i].name == imageName)
                return i < _prefabsToSpawn.Length ? _prefabsToSpawn[i] : null;
        }

        return null;
    }
}
