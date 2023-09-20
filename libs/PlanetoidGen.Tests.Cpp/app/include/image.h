#ifndef IMAGE_H
#define IMAGE_H

// base class for working with images (png/raw)

#include <stdio.h>
#include <string>

typedef enum {FORMAT_TGA, FORMAT_PNG, FORMAT_JPG} ImageFormat;

typedef unsigned int uint;
typedef unsigned char uchar;
typedef unsigned long ulong;
typedef unsigned int uint;

const int origin_top = 0;
const int origin_bottom = 1;

const int rgba_8 = 0;
const int lum_16 = 1;
const int grey_8 = 2;

class Image
{
private:
	uint size_x;
	uint size_y;

	unsigned long **map;
	unsigned char origin;
	unsigned char mode;
	ImageFormat format;
	std::string name;
	int page_size;

	unsigned long max, min;

	void allocate (uint x, uint y);
	void release ();

	unsigned char *BuildByteSeq (uint x1, uint y1, uint x2, uint y2,
		float scale=1);
	unsigned short *BuildShortSeq (uint x1, uint y1, uint x2, uint y2,
		float scale=1);
	void write_page (int page_x, int page_y);
	std::string getExtension ();

public:
	Image (uint x, uint y);
	Image ();
	Image (const char *filename);
	virtual ~Image ();

	inline bool in_range (int x, int y)
	{
		if ((x < 0) || (y < 0) || (x >= (int) size_x) || (y >= (int) size_y))
			return false;
		else
			return true;
	}

	bool in_f_range (float x_percent, float y_percent);

	inline uint GetXSize () {return size_x;}
	inline uint GetYSize () {return size_y;}

	void Write (const char *filename, float scale=1);
	void Write (const char *filename, uint x1, uint y1, uint x2, uint y2, 
		float scale = 1);
	void SplitImage (int page_size);

	void Load (const char *filename);

	virtual void Set (uint x, uint y, unsigned long value);
	void fSet (float x, float y, unsigned long value);
	unsigned long Get (int x, int y);
	unsigned long fGet (float x, float y);

	inline void SetOrigin (const int o) {origin = o;}
	inline void SetMode (const int m) { mode = m;}
	inline void SetFormat (ImageFormat f)	{format = f;}
	inline void SetName (std::string n)		{name = n;}
};

#endif
