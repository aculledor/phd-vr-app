/************************************************************************************

Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK License Version 3.4.1 (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if PRIORITIZE_OCULUS_XR_SETTINGS
using Unity.XR.Oculus;
#endif

public class OVRDeviceSelector
{
	public static bool isTargetDeviceQuestFamily
	{
		get
		{
			return isTargetDeviceQuest || isTargetDeviceQuest2;
		}
	}
	static bool IsProjectConfigTargetDeviceEnabled(OVRProjectConfig.DeviceType deviceType)
	{
		OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();
		return projectConfig.targetDeviceTypes.Contains(deviceType);
	}

#if PRIORITIZE_OCULUS_XR_SETTINGS
	static bool IsOculusSettingsTargetDeviceEnabled(string propertyName, OVRProjectConfig.DeviceType fallbackDeviceType)
	{
		OculusSettings settings;
		UnityEditor.EditorBuildSettings.TryGetConfigObject<OculusSettings>("Unity.XR.Oculus.Settings", out settings);
		if (settings != null)
		{
			var property = typeof(OculusSettings).GetProperty(propertyName);
			if (property != null && property.PropertyType == typeof(bool))
			{
				return (bool)property.GetValue(settings, null);
			}
		}

		return IsProjectConfigTargetDeviceEnabled(fallbackDeviceType);
	}
#endif

	public static bool isTargetDeviceQuest
	{
		get
		{
#if PRIORITIZE_OCULUS_XR_SETTINGS
			return IsOculusSettingsTargetDeviceEnabled("TargetQuest", OVRProjectConfig.DeviceType.Quest);
#else
			return IsProjectConfigTargetDeviceEnabled(OVRProjectConfig.DeviceType.Quest);
#endif

		}
	}

	public static bool isTargetDeviceQuest2
	{
		get
		{
#if PRIORITIZE_OCULUS_XR_SETTINGS
			return IsOculusSettingsTargetDeviceEnabled("TargetQuest2", OVRProjectConfig.DeviceType.Quest2);
#else
			return IsProjectConfigTargetDeviceEnabled(OVRProjectConfig.DeviceType.Quest2);
#endif
		}
	}
}
