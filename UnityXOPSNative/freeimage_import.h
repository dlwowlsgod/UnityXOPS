#pragma once

struct ImageData {
	int width;
	int height;
	unsigned char* pixelData;
};

enum ImageImportProfile : unsigned int {
	IMPORT_ABORT = 0,
	IMPORT_JPG = 1,
	IMPORT_PNG = 2,
	IMPORT_BMP = 3,
	IMPORT_TGA = 4,
	IMPORT_DDS = 5
};

extern "C" {
	__declspec(dllexport) ImageData* ImportImage(const char* filePath, ImageImportProfile profile);
	__declspec(dllexport) void DeAllocImage(ImageData* data);
	__declspec(dllexport) const char* GetFreeImageVersion();
}