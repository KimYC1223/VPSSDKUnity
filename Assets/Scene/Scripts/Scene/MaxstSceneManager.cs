﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using maxstAR;
using UnityEngine.UI;
using System.IO;
using System;

public class MaxstSceneManager : MonoBehaviour
{
	private CameraBackgroundBehaviour cameraBackgroundBehaviour = null;
	private GameObject arCamera = null;
	private VPSStudioController vPSStudioController = null;

	public List<GameObject> disableObjects = new List<GameObject>();
	public GameObject rootTrackable;
	public List<GameObject> occlusionObjects = new List<GameObject>();

	public Material buildingMaterial;
	public Material runtimeBuildingMaterial;

	public GameObject maxstLogObject;

	private string serverName = "";

	void Awake()
	{
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;

		AndroidRuntimePermissions.Permission[] result = AndroidRuntimePermissions.RequestPermissions("android.permission.WRITE_EXTERNAL_STORAGE", "android.permission.CAMERA", "android.permission.ACCESS_FINE_LOCATION", "android.permission.ACCESS_COARSE_LOCATION");
		if (result[0] == AndroidRuntimePermissions.Permission.Granted && result[1] == AndroidRuntimePermissions.Permission.Granted)
			Debug.Log("We have all the permissions!");
		else
			Debug.Log("Some permission(s) are not granted...");

		ARManager arManagr = FindObjectOfType<ARManager>();
		if (arManagr == null)
		{
			Debug.LogError("Can't find ARManager. You need to add ARManager prefab in scene.");
			return;
		}
		else
		{
			arCamera = arManagr.gameObject;
		}
		
		vPSStudioController = FindObjectOfType<VPSStudioController>();
		if (vPSStudioController == null)
		{
			Debug.LogError("Can't find VPSStudioController. You need to add VPSStudio prefab in scene.");
			return;
		}
		else
        {
			string tempServerName = vPSStudioController.vpsServerName;
			serverName = tempServerName;
			vPSStudioController.gameObject.SetActive(false);
		}

		cameraBackgroundBehaviour = FindObjectOfType<CameraBackgroundBehaviour>();
		if (cameraBackgroundBehaviour == null)
		{
			Debug.LogError("Can't find CameraBackgroundBehaviour.");
			return;
		}

		if(rootTrackable == null)
        {
			Debug.LogError("You need to add RootTrackable.");
		}

		foreach (GameObject eachObject in disableObjects)
		{
			if(eachObject != null)
            {
				eachObject.SetActive(false);
			}
		}
	}

	void Start()
	{
		foreach (GameObject eachGameObject in occlusionObjects)
		{
			if (eachGameObject == null)
			{
				continue;
			}

			Renderer[] cullingRenderer = eachGameObject.GetComponentsInChildren<Renderer>();
			foreach (Renderer eachRenderer in cullingRenderer)
			{
				eachRenderer.material.renderQueue = 1900;
				eachRenderer.material = runtimeBuildingMaterial;
			}
		}

		if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
        {
			string simulatePath = vPSStudioController.vpsSimulatePath;
			if (Directory.Exists(simulatePath))
			{
				CameraDevice.GetInstance().Start(simulatePath);
				MaxstAR.SetScreenOrientation((int)ScreenOrientation.Portrait);
			}
		}
		else
        {
			if (CameraDevice.GetInstance().IsFusionSupported(CameraDevice.FusionType.ARCamera))
			{
				CameraDevice.GetInstance().Start();
			}
			else
			{
				TrackerManager.GetInstance().RequestARCoreApk();
			}
		}
		
		TrackerManager.GetInstance().StartTracker();

		if (serverName != "")
		{
			string vpsquery = "{\"vps_server\":\"" + serverName + "\"}";
			TrackerManager.GetInstance().AddTrackerData(vpsquery);
		}
	}

	void Update()
	{
		TrackerManager.GetInstance().UpdateFrame();

		ARFrame arFrame = TrackerManager.GetInstance().GetARFrame();
		
		TrackedImage trackedImage = arFrame.GetTrackedImage();


        if (trackedImage.IsTextureId())
        {
            IntPtr[] cameraTextureIds = trackedImage.GetTextureIds();
            cameraBackgroundBehaviour.UpdateCameraBackgroundImage(cameraTextureIds);
        }
        else
        {
            cameraBackgroundBehaviour.UpdateCameraBackgroundImage(trackedImage);
        }

		if(arFrame.GetARLocationRecognitionState() == ARLocationRecognitionState.ARLocationRecognitionStateNormal)
        {
			Matrix4x4 targetPose = arFrame.GetTransform();

			arCamera.transform.position = MatrixUtils.PositionFromMatrix(targetPose);
			arCamera.transform.rotation = MatrixUtils.QuaternionFromMatrix(targetPose);
			arCamera.transform.localScale = MatrixUtils.ScaleFromMatrix(targetPose);

			rootTrackable.SetActive(true);
		}
		else
        {
			rootTrackable.SetActive(false);
		}
	}

	void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			CameraDevice.GetInstance().Stop();
			TrackerManager.GetInstance().StopTracker();
		}
		else
		{
			if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
			{
				string simulatePath = vPSStudioController.vpsSimulatePath;
				if (Directory.Exists(simulatePath))
				{
					CameraDevice.GetInstance().Start(simulatePath);
					MaxstAR.SetScreenOrientation((int)ScreenOrientation.Portrait);
				}
			}
			else
			{
				if (CameraDevice.GetInstance().IsFusionSupported(CameraDevice.FusionType.ARCamera))
				{
					CameraDevice.GetInstance().Start();
				}
				else
				{
					TrackerManager.GetInstance().RequestARCoreApk();
				}
			}

			TrackerManager.GetInstance().StartTracker();
			if (serverName != "")
			{
				string vpsquery = "{\"vps_server\":\"" + serverName + "\"}";
				TrackerManager.GetInstance().AddTrackerData(vpsquery);
			}
		}
	}

	void OnDestroy()
	{
		CameraDevice.GetInstance().Stop();
		TrackerManager.GetInstance().StopTracker();
		TrackerManager.GetInstance().DestroyTracker();
	}

	public void OnClickNavigation()
    {
		NavigationController navigationController = vPSStudioController.GetComponent<NavigationController>();
		navigationController.MakePath(arCamera.transform.position, new Vector3(77.975977f, 0, 71.859565f), serverName);
    }

	void FixedUpdate()
	{
		if (Input.GetMouseButtonUp(0))
		{
			AttachLogo();
			
		}
	}

	public void AttachLogo()
    {
		Vector2 vTouchPos = Input.mousePosition;

		Ray ray = Camera.main.ScreenPointToRay(vTouchPos);

		RaycastHit vHit;
		if (Physics.Raycast(ray.origin, ray.direction, out vHit))
		{
			maxstLogObject.transform.position = vHit.point;
			maxstLogObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, vHit.normal) * Quaternion.Euler(-90.0f ,0.0f, 0.0f);
		}
	}
}