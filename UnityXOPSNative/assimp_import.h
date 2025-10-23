#pragma once

struct Vector3 {
	float x, y, z;
};

struct Vector2 {
	float x, y;
};

struct MeshData {
	Vector3* vertices;
	int vertexCount;
	Vector2* uvs;
	int uvCount;
	int* indices;
	int indexCount;
};

enum ModelImportProfile : unsigned int {
	IMPORT_ABORT = 0,
	IMPORT_XFILE = 1,
	IMPORT_XFILE_FIXTOKEN = 2
};

extern "C" {
	__declspec(dllexport) MeshData* ImportModel(const char* filePath, ModelImportProfile profile);
	__declspec(dllexport) void DeAllocModel(MeshData* data);
	__declspec(dllexport) void GetAssimpVersion(unsigned int* major, unsigned int* minor, unsigned int* patch, unsigned int* revision);
}