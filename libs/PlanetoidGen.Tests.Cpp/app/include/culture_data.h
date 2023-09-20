#ifndef CULTURE_DATA_H
#define CULTURE_DATA_H

struct culture_header
{
	long index_size;
};

struct culture_index
{
	char  name[80];
};

#define CULTURE_MESH		0
#define CULTURE_OCEANPLANE	1

struct culture_type
{
	short type;
};

struct culture_mesh
{
	long id;
	float x;
	float y;
	float z;
	float yaw;
	float scale;
};

struct culture_oceanplane
{
	float x1;
	float y1;
	float x2;
	float y2;
	float altitude;
};

#endif
