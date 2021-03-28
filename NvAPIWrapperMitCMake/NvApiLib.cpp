#include <stdio.h>
//#include "stdafx.h"
#include <windows.h>
//#include <pch.h>
#include "NvApiLib.h"
#include "include\nvapi.h"
#include <cstdio>
#include <iostream>

using namespace std;

void GetError(NvAPI_Status errorcode, Error* error) {
	NvAPI_GetErrorMessage(errorcode, error->Message);
}


/*int TestWarp() {
	NvAPI_Status error;
	NvPhysicalGpuHandle nvGPUHandles[NVAPI_MAX_PHYSICAL_GPUS];
	NvU32 gpuCount = 0;
	NvU32 gpu;
	NvU32 outputMask = 0;
	NvSBox desktopRect;
	NvSBox scanoutRect; //portion of the desktop
	NvSBox viewportRect; //the viewport which is a subregion of the scanout
	NvSBox osRect; //os coordinates of the desktop

	NV_SCANOUT_WARPING_DATA warpingData;
	NvAPI_ShortString estring;
	int maxNumVertices = 0;
	int sticky = 0;

	printf("App Version: 1.3\n");

	// Initialize NVAPI, get GPU handles, etc.
	error = NvAPI_Initialize();
	ZeroMemory(&nvGPUHandles, sizeof(nvGPUHandles));
	error = NvAPI_EnumPhysicalGPUs(nvGPUHandles, &gpuCount);

	// At this point we have a list of accessible physical nvidia gpus in the system.
	// Loop over all gpus

	for (gpu = 0; gpu < gpuCount; gpu++)
	{
		NvU32 dispIdCount = 0;

		// Query the active physical display connected to each gpu.
		error = NvAPI_GPU_GetConnectedDisplayIds(nvGPUHandles[gpu], NULL, &dispIdCount, 0);
		if ((error != NVAPI_OK) || (dispIdCount == 0))
		{
			NvAPI_GetErrorMessage(error, estring);
			printf("NvAPI_GPU_GetConnectedDisplayIds: %s\n", estring);
			printf("Display count %d\n", dispIdCount);
			return error;
		}

		NV_GPU_DISPLAYIDS* dispIds = NULL;
		dispIds = new NV_GPU_DISPLAYIDS[dispIdCount];
		dispIds->version = NV_GPU_DISPLAYIDS_VER;
		error = NvAPI_GPU_GetConnectedDisplayIds(nvGPUHandles[gpu], dispIds, &dispIdCount, 0);
		if (error != NVAPI_OK)
		{
			delete[] dispIds;
			NvAPI_GetErrorMessage(error, estring);
			printf("NvAPI_GPU_GetConnectedDisplayIds: %s\n", estring);
		}


		for (int dispIndex = 0; (dispIndex < dispIdCount) && dispIds[dispIndex].isActive; dispIndex++)
		{

			ZeroMemory(&desktopRect, sizeof(desktopRect));
			ZeroMemory(&scanoutRect, sizeof(scanoutRect));
			ZeroMemory(&osRect, sizeof(osRect));
			ZeroMemory(&viewportRect, sizeof(viewportRect));
			printf("GPU %d, displayId 0x%08x\n", gpu, dispIds[dispIndex].displayId);

			// Query the desktop and scanout portion of each physical active display.
			error = NvAPI_GPU_GetScanoutConfiguration(dispIds[dispIndex].displayId, &desktopRect, &scanoutRect);
			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_GetScanoutConfiguration: %s\n", estring);
			}


			//The below is optional for R331+ in cases where the viewport rect!=scanoutRect
			NV_SCANOUT_INFORMATION scanoutInformation;
			scanoutInformation.version = NV_SCANOUT_INFORMATION_VER;
			error = NvAPI_GPU_GetScanoutConfigurationEx(dispIds[dispIndex].displayId, &scanoutInformation);
			//if this new interface is supported fetch scanout data from it
			if (error == NVAPI_OK)
			{
				scanoutRect.sWidth = scanoutInformation.targetDisplayWidth;
				scanoutRect.sHeight = scanoutInformation.targetDisplayHeight;
				viewportRect = scanoutInformation.targetViewportRect;
			}

			//Need to get osRect for this we need the NvDisplayHandle to get access to the displayName to be pased to win32
			NvDisplayHandle disp = NULL;
			NvAPI_DISP_GetDisplayHandleFromDisplayId(dispIds[dispIndex].displayId, &disp);
			//TODO errhandle
			NvAPI_ShortString displayName;
			error = NvAPI_GetAssociatedNvidiaDisplayName(disp, displayName);
			//TODO errhandle

			DEVMODEA dm = { 0 };
			dm.dmSize = sizeof(DEVMODEA);
			if (!EnumDisplaySettingsA(displayName, ENUM_CURRENT_SETTINGS, &dm)) {
				//TODO handle error
			}

			osRect.sX = dm.dmPosition.x;
			osRect.sY = dm.dmPosition.y;
			osRect.sWidth = dm.dmPelsWidth;
			osRect.sHeight = dm.dmPelsHeight;

			printf(" desktopRect: sX = %6d, sY = %6d, sWidth = %6d sHeight = %6d\n", desktopRect.sX, desktopRect.sY, desktopRect.sWidth, desktopRect.sHeight);
			printf(" scanoutRect: sX = %6d, sY = %6d, sWidth = %6d sHeight = %6d\n", scanoutRect.sX, scanoutRect.sY, scanoutRect.sWidth, scanoutRect.sHeight);
			printf(" viewportRect: sX = %6d, sY = %6d, sWidth = %6d sHeight = %6d\n", viewportRect.sX, viewportRect.sY, viewportRect.sWidth, viewportRect.sHeight);
			printf(" osRect: sX = %6d, sY = %6d, sWidth = %6d sHeight = %6d\n", osRect.sX, osRect.sY, osRect.sWidth, osRect.sHeight);


			// desktopRect should now contain the area of the desktop which is scanned
			// out to the display given by the displayId.
			// scanoutRect should now contain the area of the scanout to which the 
			// desktopRect is scanned out. That will give information about the gpu 
			// internal output scaling before applying the warping and intensity control
			// However that doesn't give information about the scaling performed on 
			// the display side.

			// With this information the intensity and warping coordinates can be 
			// computed. The example here warps the desktopRect to a trapezoid similar
			// to the one seen in the diagram.	


			// warp vertices are defined in scanoutRect coordinates    					
			float	dstWidth = scanoutRect.sWidth / 2.0f;
			float	dstHeight = scanoutRect.sHeight;
			float	dstXShift = dstWidth / 2.0f;
			float	dstYShift = dstHeight / 2.0f;
			float	dstLeft = (float)scanoutRect.sX + dstXShift;
			float	dstTop = (float)scanoutRect.sY; //TODO play

			// Triangle strip with 4 vertices
			// The vertices are given as a 2D vertex strip because the warp
			// is a 2d operation. To be able to emulate 3d perspective correction, 
			// the texture coordinate contains 4 components, which needs to be
			// adjusted to get this correction.
			//
			// A trapezoid needs 4 vertices.
			// Format is xy for the vertex + yurq for the texture coordinate.
			// So we need 24 floats for that.
			//float vertices [4*6];
			//
			//  (0)  ----------------  (2)
			//       |             / |
			//       |            /  |
			//       |           /   |
			//       |          /    |
			//       |         /     |
			//       |        /      |
			//       |       /       |
			//       |      /        | 
			//       |     /         |
			//       |    /          |
			//       |   /           |
			//       |  /            |
			//       | /             |  
			//   (1) |---------------- (3)
			//


			// texture coordinates
			// warp texture coordinates are defined in desktopRect coordinates
			float srcLeft = (float)desktopRect.sX;
			float srcTop = (float)desktopRect.sY;
			float srcWidth = desktopRect.sWidth;
			float srcHeight = desktopRect.sHeight;

			float vertices[] =
				//     x                        y                  u                           v                   r       q
			{

				dstLeft,				     dstTop,			srcLeft,					srcTop,				0.0f,	 1.0f,     // 0
				dstLeft - dstXShift,           dstTop + dstHeight,	srcLeft,					srcTop + srcHeight,	0.0f,	 1.0f,     // 1
				dstLeft + dstWidth + dstXShift,	 dstTop,			srcLeft + srcWidth,			srcTop,				0.0f,	 1.0f,      // 4
				dstLeft + dstWidth,	         dstTop + dstHeight,	srcLeft + srcWidth,			srcTop + srcHeight,	0.0f,	 1.0f,     // 3
			};
			int maxnumvert = 4;

			printf("vertices: %6.0f, %6.0f, %6.0f, %6.0f, %6.0f, %6.0f\n", vertices[0], vertices[1], vertices[2], vertices[3], vertices[4], vertices[5]);
			printf("vertices: %6.0f, %6.0f, %6.0f, %6.0f, %6.0f, %6.0f\n", vertices[6], vertices[7], vertices[8], vertices[9], vertices[10], vertices[11]);
			printf("vertices: %6.0f, %6.0f, %6.0f, %6.0f, %6.0f, %6.0f\n", vertices[12], vertices[13], vertices[14], vertices[15], vertices[16], vertices[17]);
			printf("vertices: %6.0f, %6.0f, %6.0f, %6.0f, %6.0f, %6.0f\n", vertices[18], vertices[19], vertices[20], vertices[21], vertices[22], vertices[23]);
			//	printf("vertices: %6.0f, %6.0f, %6.0f, %6.0f, %6.0f, %6.0f\n",vertices[24],vertices[25],vertices[26],vertices[27],vertices[28],vertices[29]);

			// Demo Warping
			printf("Demo Warping\n");
			warpingData.version = NV_SCANOUT_WARPING_VER;
			warpingData.numVertices = maxnumvert;
			warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
			warpingData.textureRect = &osRect;
			warpingData.vertices = vertices;

			// This call does the Warp
			error = NvAPI_GPU_SetScanoutWarping(dispIds[dispIndex].displayId, &warpingData, &maxNumVertices, &sticky);
			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutWarping: %s\n", estring);
			}

			system("pause");

			warpingData.vertices = NULL;
			warpingData.numVertices = 0;

			error = NvAPI_GPU_SetScanoutWarping(dispIds[dispIndex].displayId, &warpingData, &maxNumVertices, &sticky);

			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutWarping: %s\n", estring);
			}

			// Demo Warp and Blend Antialiasing
			printf("Demo Warping and Blend Antialiasing\n");

			warpingData.version = NV_SCANOUT_WARPING_VER;
			warpingData.numVertices = maxnumvert;
			warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
			warpingData.textureRect = &osRect;
			warpingData.vertices = vertices;
			// This call does the Warp
			error = NvAPI_GPU_SetScanoutWarping(dispIds[dispIndex].displayId, &warpingData, &maxNumVertices, &sticky);
			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutWarping: %s\n", estring);
			}

			// This call changes the warp filter function to bicubic triangular
			printf("Changing warp resample method to  bicubic triangular\n");
			float unusedContainer;
			NV_GPU_SCANOUT_COMPOSITION_PARAMETER_VALUE parameterValue = NV_GPU_SCANOUT_COMPOSITION_PARAMETER_VALUE_WARPING_RESAMPLING_METHOD_BICUBIC_TRIANGULAR;
			error = NvAPI_GPU_SetScanoutCompositionParameter(dispIds[dispIndex].displayId, NV_GPU_SCANOUT_COMPOSITION_PARAMETER_WARPING_RESAMPLING_METHOD, parameterValue, &unusedContainer);

			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutCompositionParameter: %s\n", estring);
			}

			system("pause");

			// After a pause, set back to unwarped and un-antialiased
			parameterValue = NV_GPU_SCANOUT_COMPOSITION_PARAMETER_VALUE_WARPING_RESAMPLING_METHOD_BILINEAR;
			error = NvAPI_GPU_SetScanoutCompositionParameter(dispIds[dispIndex].displayId, NV_GPU_SCANOUT_COMPOSITION_PARAMETER_WARPING_RESAMPLING_METHOD, parameterValue, &unusedContainer);

			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutCompositionParameter: %s\n", estring);
			}

			warpingData.vertices = NULL;
			warpingData.numVertices = 0;

			error = NvAPI_GPU_SetScanoutWarping(dispIds[dispIndex].displayId, &warpingData, &maxNumVertices, &sticky);

			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutWarping: %s\n", estring);
			}

			// Demonstration of Intensity adjustment
			printf("Demo Intensity Adjustment\n");

			NV_SCANOUT_INTENSITY_DATA intensityData;
			// As per pixel intensity control example we specify only one intensity 
			// value which is interpolated over the scanout area and affecting 
			// all pixels by that. It's also possible to specify much more intensity
			// values than scanout pixels.

			float intensityTexture[12] = { 0.95f, 0.95f, 0.95f, 0.75f, 0.75f, 0.75f, 0.25f, 0.25f, 0.25f, 0.05f, 0.05f, 0.05f };

			float offsetTexture[2] = { 0.0f, 0.1f };
			intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
			intensityData.width = 4;
			intensityData.height = 1;
			intensityData.blendingTexture = intensityTexture;
			intensityData.offsetTexture = offsetTexture;
			intensityData.offsetTexChannels = 1;

			int sticky = 0;

			// This call does the intensity map
			error = NvAPI_GPU_SetScanoutIntensity(dispIds[dispIndex].displayId, &intensityData, &sticky);

			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutIntensity: %s\n", estring);
			}

			system("pause");

			// After a pause, set back to normal intensity

			intensityData.blendingTexture = NULL;

			error = NvAPI_GPU_SetScanoutIntensity(dispIds[dispIndex].displayId, &intensityData, &sticky);
			if (error != NVAPI_OK)
			{
				NvAPI_GetErrorMessage(error, estring);
				printf("NvAPI_GPU_SetScanoutIntensity: %s\n", estring);
			}
		} //end of for displays
		delete[] dispIds;
	}	//end of loop gpus

	return 1;
}*/

int Initialize() {
	//FILELog::ReportingLevel() = logDEBUG;
	//FILE* log_fd;
	//fopen_s(&log_fd, "C:\\Temp\\nvapi.log", "w");
	//Output2FILE::Stream() = log_fd;
	//FILE_LOG(logINFO) << "Initialize";
	NvAPI_Status error;
	error = NvAPI_Initialize();
	if (error != NVAPI_OK)
	{
		return error;
	}
	return NVAPI_OK;
}

int GetMosaicInfo(MosaicInfo* mosaicInfo) {
	NvAPI_Status error;
	NV_MOSAIC_TOPO_BRIEF  topo;
	topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;
	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;
	NvS32 overlapX, overlapY;
	error = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
	if (error != NVAPI_OK)
	{
		return error;
	}
	mosaicInfo->Overlap = overlapX;

	NvU32 gridCount = 0;
	error = NvAPI_Mosaic_EnumDisplayGrids(NULL, &gridCount);
	if (error != NVAPI_OK)
	{
		return error;
	}
	NV_MOSAIC_GRID_TOPO* grids = NULL;
	grids = new NV_MOSAIC_GRID_TOPO[gridCount];
	grids->version = NV_MOSAIC_GRID_TOPO_VER;
	error = NvAPI_Mosaic_EnumDisplayGrids(grids, &gridCount);
	if (error != NVAPI_OK) {
		return error;
	}
	for (NvU32 i = 0; i < gridCount; i++) {
		if (grids[i].displayCount != 2) {
			continue;
		}
		mosaicInfo->ProjectorWidth = grids[i].displaySettings.width;
		mosaicInfo->ProjectorHeight = grids[i].displaySettings.height;
		mosaicInfo->DisplayId0 = grids[i].displays[0].displayId;
		mosaicInfo->DisplayId1 = grids[i].displays[1].displayId;
		break;
	}
	return error;
}

int Warp(NvU32 displayId, float vertices[], int numVertices) {
	NvAPI_Status error;
	NV_SCANOUT_WARPING_DATA warpingData;
	int maxNumVertices = 0;
	int sticky = 0;

	NV_SCANOUT_INFORMATION scanInfo;

	ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
	scanInfo.version = NV_SCANOUT_INFORMATION_VER;

	error = NvAPI_GPU_GetScanoutConfigurationEx(displayId, &scanInfo);
	if (error != NVAPI_OK)
	{
		return error;
	}

	NV_MOSAIC_TOPO_BRIEF  topo;
	topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;

	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;

	NvS32 overlapX, overlapY;
	float srcLeft, srcTop, srcWidth, srcHeight;


	error = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
	if (error != NVAPI_OK)
	{
		return error;
	}

	if (topo.enabled == false)
	{
		srcLeft = (float)scanInfo.sourceDesktopRect.sX;
		srcTop = (float)scanInfo.sourceDesktopRect.sY;
		srcWidth = (float)scanInfo.sourceDesktopRect.sWidth;
		srcHeight = (float)scanInfo.sourceDesktopRect.sHeight;
	}
	else
	{
		srcLeft = (float)scanInfo.sourceViewportRect.sX;
		srcTop = (float)scanInfo.sourceViewportRect.sY;
		srcWidth = (float)scanInfo.sourceViewportRect.sWidth;
		srcHeight = (float)scanInfo.sourceViewportRect.sHeight;
	}

	warpingData.version = NV_SCANOUT_WARPING_VER;
	warpingData.numVertices = numVertices;
	warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
	warpingData.textureRect = &scanInfo.sourceDesktopRect;
	warpingData.vertices = &vertices[0];

	error = NvAPI_GPU_SetScanoutWarping(displayId, &warpingData, &maxNumVertices, &sticky);
	if (error != NVAPI_OK)
	{
		return error;
	}


	return error;
}

int WarpMultiple(NvU32 displayIds[], int count, float vertices[], int numVertices) {
	NvAPI_Status status;
	int sticky;
	int maxNumVertices;
	NV_SCANOUT_INFORMATION scanInfo;
	NV_SCANOUT_WARPING_DATA warpingData;
	NV_MOSAIC_TOPO_BRIEF topo;
	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	NvS32 overlapX, overlapY;
	float srcLeft, srcTop, srcWidth, srcHeight;

	// Mit dem Warping des 2. Beamers gibt es irgend ein Problem. Entweder muss 2 Mal gewarpt werden,
	// oder es könnte auch zuerst der zweite Beamer gewarpt werden.
	//for(int i = 1;i>=0;i--) {
	for (int i = 0; i < count; i++) {

		maxNumVertices = 0;
		sticky = 0;

		ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
		scanInfo.version = NV_SCANOUT_INFORMATION_VER;

		ZeroMemory(&warpingData, sizeof(NV_SCANOUT_WARPING_DATA));
		ZeroMemory(&topo, sizeof(NV_MOSAIC_TOPO_BRIEF));
		ZeroMemory(&dispSetting, sizeof(NV_MOSAIC_DISPLAY_SETTING));

		status = NvAPI_GPU_GetScanoutConfigurationEx(displayIds[i], &scanInfo);
		if (status != NVAPI_OK)
		{
			return status;
		}

		topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;

		dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;

		status = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
		if (status != NVAPI_OK)
		{
			return status;
		}

		if (topo.enabled == false)
		{
			srcLeft = (float)scanInfo.sourceDesktopRect.sX;
			srcTop = (float)scanInfo.sourceDesktopRect.sY;
			srcWidth = (float)scanInfo.sourceDesktopRect.sWidth;
			srcHeight = (float)scanInfo.sourceDesktopRect.sHeight;
		}
		else
		{
			srcLeft = (float)scanInfo.sourceViewportRect.sX;
			srcTop = (float)scanInfo.sourceViewportRect.sY;
			srcWidth = (float)scanInfo.sourceViewportRect.sWidth;
			srcHeight = (float)scanInfo.sourceViewportRect.sHeight;
		}		

		warpingData.version = NV_SCANOUT_WARPING_VER;
		warpingData.numVertices = numVertices;
		warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
		warpingData.textureRect = &scanInfo.sourceDesktopRect;

		warpingData.vertices = &vertices[i*numVertices*6];

		// 2 Mal warpen, siehe Kommentar oben.
		for (int j = 0; j <= 1; j++) {
			status = NvAPI_GPU_SetScanoutWarping(displayIds[i], &warpingData, &maxNumVertices, &sticky);
			if (status != NVAPI_OK)
			{
				return status;
			}
		}
#ifdef TRIAL
	int count = (int)srcWidth*(int)srcHeight * 3;
	float* image = new float[count];
	for (int i = 0; i < count; i++) {
		image[i] = 1;
	}

	ShowImage(displayId, image, (int)srcWidth, (int)srcHeight);

	delete[] image;
#endif

	}
	return status;
}

int Blend(NvU32 displayId, float blend[], float offset[], int width, int height) {
#ifdef TRIAL
	int x0 = 0, x1 = width - 1;
	int y0 = 0, y1 = height - 1;
	int dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	int dy = -y1, sy = y0 < y1 ? 1 : -1;
	int err = dx + dy, e2;

	while (1) {		
		blend[(x0 + y0 * width) * 3 + 0] = 1;
		blend[(x0 + y0 * width) * 3 + 1] = 0;
		blend[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}

	x0 = 0, x1 = width - 1;
	y0 = height - 1, y1 = 0;
	dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	dy = -y0, sy = y0 < y1 ? 1 : -1;
	err = dx + dy, e2;

	while (1) {
		blend[(x0 + y0 * width) * 3 + 0] = 1;
		blend[(x0 + y0 * width) * 3 + 1] = 0;
		blend[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}
#endif
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.width = width;
	intensityData.height = height;
	intensityData.blendingTexture = blend;
	intensityData.offsetTexture = offset;
	intensityData.offsetTexChannels = 1;
	int sticky = 0;
	return NvAPI_GPU_SetScanoutIntensity(displayId, &intensityData, &sticky);
}


int UnWarp(NvU32 displayIds[], int count) {
	NV_SCANOUT_WARPING_DATA warpingData;
	NV_SCANOUT_INFORMATION scanInfo;
	int maxNumVertices = 0;
	int sticky = 0;
	NvAPI_Status status;

	ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
	scanInfo.version = NV_SCANOUT_INFORMATION_VER;

	warpingData.version = NV_SCANOUT_WARPING_VER;
	warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
	warpingData.textureRect = &scanInfo.sourceDesktopRect;
	warpingData.vertices = NULL;
	warpingData.numVertices = 0;

	for (int i = 0; i < count; i++)
	{
		status = NvAPI_GPU_SetScanoutWarping(displayIds[i], &warpingData, &maxNumVertices, &sticky);
		if (status != NVAPI_OK)
		{
			return status;
		}
	}
	return NVAPI_OK;
}

int UnBlend(NvU32 displayIds[], int count, int width, int height) {
	int sticky = 0;
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.blendingTexture = NULL;
	intensityData.width = width;
	intensityData.height = height;
	NvAPI_Status status;

	intensityData.offsetTexture = NULL;
	intensityData.offsetTexChannels = 1;

	for (int i = 0; i < count; i++)
	{
		status = NvAPI_GPU_SetScanoutIntensity(displayIds[i], &intensityData, &sticky);
		if (status != NVAPI_OK)
		{
			return status;
		}
	}
	return NVAPI_OK;
}

int ShowImage(NvU32 displayId, float image[], int width, int height) {
#ifdef TRIAL
	int x0 = 0, x1 = width - 1;
	int y0 = 0, y1 = height - 1;
	int dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	int dy = -y1, sy = y0 < y1 ? 1 : -1;
	int err = dx + dy, e2;

	while (1) {
		image[(x0 + y0 * width) * 3 + 0] = 1;
		image[(x0 + y0 * width) * 3 + 1] = 0;
		image[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}

	x0 = 0, x1 = width - 1;
	y0 = height - 1, y1 = 0;
	dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	dy = -y0, sy = y0 < y1 ? 1 : -1;
	err = dx + dy, e2;

	while (1) {
		image[(x0 + y0 * width) * 3 + 0] = 1;
		image[(x0 + y0 * width) * 3 + 1] = 0;
		image[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}
#endif
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.width = width;
	intensityData.height = height;
	intensityData.blendingTexture = image;
	intensityData.offsetTexture = NULL;
	intensityData.offsetTexChannels = 1;
	int sticky = 0;
	return NvAPI_GPU_SetScanoutIntensity(displayId, &intensityData, &sticky);
}
