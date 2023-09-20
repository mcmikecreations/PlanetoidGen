#include "image.h"
#include <stdlib.h>

#include "stb_image.h"
#include "stb_image_write.h"

#include <cstdarg>
#include <math.h>
#include "logger.h"
#include <sstream>
#if defined _linux or defined __unix__ or defined __linux__ or defined __unix or defined __linux
#include <unistd.h>
#endif
#include <params.h>

using namespace std;

Image::Image (uint x, uint y)
{
	size_x = x;
	size_y = y;

	map = NULL;
	allocate (x, y);
	origin = origin_top;
	mode = rgba_8;
	format = FORMAT_JPG;

	max = 0;
	min = 0;
}

Image::Image (const char *filename)
{
	map = NULL;
	Load (const_cast <char *> (filename));
	origin = origin_top;
	mode = rgba_8;
	max = 0;
	min = 0;
}

Image::Image ()
{
	size_x = 0;
	size_y = 0;
	map = NULL;
	origin = origin_top;
	mode = rgba_8;
	max = 0;
	min = 0;
}

Image::~Image ()
{
	release ();
}


#ifdef _WIN32
double rint( double x)
// Copyright (C) 2001 Tor M. Aamodt, University of Toronto
// Permisssion to use for all purposes commercial and otherwise granted.
// THIS MATERIAL IS PROVIDED "AS IS" WITHOUT WARRANTY, OR ANY CONDITION OR
// OTHER TERM OF ANY KIND INCLUDING, WITHOUT LIMITATION, ANY WARRANTY
// OF MERCHANTABILITY, SATISFACTORY QUALITY, OR FITNESS FOR A PARTICULAR
// PURPOSE.
{
    if( x > 0 ) {
        __int64 xint = (__int64) (x+0.5);
        if( xint % 2 ) {
            // then we might have an even number...
            double diff = x - (double)xint;
            if( diff == -0.5 )
                return double(xint-1);
        }
        return double(xint);
    } else {
        __int64 xint = (__int64) (x-0.5);
        if( xint % 2 ) {
            // then we might have an even number...
            double diff = x - (double)xint;
            if( diff == 0.5 )
                return double(xint+1);
        }
        return double(xint);
    }
}

#define unlink _unlink
#endif

void Image::release ()
{
	for (unsigned int i = 0; i < size_x; i++)
	{
		delete [] map[i];
	}
	delete [] map;

	size_x = 0;
	size_y = 0;
	map = NULL;
}

// ===================================================================
// Image::allocate -- allocate memory for the image
// ===================================================================
void Image::allocate (uint xlen, uint ylen)
{
	map = new unsigned long *[size_x];
	for (unsigned int x = 0; x < size_x; x++)
	{
		map[x] = new unsigned long[size_y*4];
	}

	for (unsigned int i = 0; i < size_y; i++)
	{
		for (unsigned int j = 0; j < size_x; j++)
		{
			map[j][i] = 0;
		}
	}
}

// ===================================================================
// in_f_range -- determine if a point is inside the image boundaries
// ===================================================================
bool Image::in_f_range (float x, float y)
{
	if ((x < 0) || (y < 0) || (x > 1) || (y > 1))
		return false;
	else
		return true;
}

// ===================================================================
// ===================================================================
void Image::Set (uint x, uint y, unsigned long value)
{
	if (! in_range (x, y))
	{
	//	Logger::Instance().Log ("Image::Set: (%d,%d) out of range\n", x, y);
		return;
	}

	map[x][y] = value;
}

// ===================================================================
// ===================================================================
void Image::fSet (float x_percent, float y_percent, unsigned long value)
{
	if (! in_f_range (x_percent, y_percent))
	{
		Logger::Instance().Log ("Set: (%f,%f) out of range\n", 
			x_percent, y_percent);
		return;
	}

	uint x = (int) rint (size_x * x_percent);
	uint y = (int) rint (size_y * y_percent);

	map[x][y] = value;
}

// ===================================================================
//  Get
/// @brief Return the value at a point
///
///	@param x,y		The coordinates of the desired point
/// @return the value at the specified point
// ===================================================================
unsigned long Image::Get (int x, int y)
{
	if (! in_range (x, y))
	{
		return 0;
	}

	return map[x][y];
}

// ===================================================================
//  fGet
/// @brief Return the value at a point
///
/// Floating point coordinates (0..1) are used so that image size is not
/// a factor.  This is similar to the way shaders access their textures.
///
///	@param x_percent,y_percent		floating point coords within image
/// @return the value at the specified point
// ===================================================================
unsigned long Image::fGet (float x_percent, float y_percent)
{
	if (! in_f_range (x_percent, y_percent))
	{
		Logger::Instance().Log ("Get: (%f,%f) out of range\n", 
			x_percent, y_percent);
		return 0;
	}

	uint x = (int) rint (size_x * x_percent);
	uint y = (int) rint (size_y * y_percent);

	return map[x][y];
}

// ===================================================================
//  BuildByteSeq
/// @brief 		Convert a 2D image to a 1D sequence of byte values
///
/// This is in preparation for writing the image out.
///
/// Inputs:
///	  x1, y1, x2, y2:   The bounding points, inclusive
///    scale:            A scaling factor which is applied to each point
// ===================================================================
unsigned char *Image::BuildByteSeq (uint x1, uint y1, uint x2, uint y2,
	float scale)
{
	uint width = (x2 - x1);
	uint height = (y2 - y1);
	uint length = width * height;

    Params& params = Params::Instance();

	Logger::Instance().Log ("building byte seq for %d x %d image\n", width, height);
	unsigned char *byte_seq = new unsigned char[length*4];

	if (byte_seq == NULL)
		return NULL;

	if (origin == origin_top)
	{
    	// convert 2d array to 1d bitmap
		for (uint y = y1; y < y2; y++)
		{
			for (uint x = x1; x < x2; x++)
			{
				unsigned long value = Get (x, y);

				unsigned char r, g, b, a;
				switch (mode)
				{
				case grey_8:
					r = (unsigned char) ((value & 0xff) * scale);
					g = (unsigned char) ((value & 0xff) * scale);
					b = (unsigned char) ((value & 0xff) * scale);
					a = (unsigned char) 255;
					break;
				case rgba_8:
				default:
                    unsigned char v = (unsigned char)(value * scale / (params.mountain_max_alt + params.hill_max_alt) * 255);
                    r = v;
                    g = v;
                    b = v;
                    a = 255;
					//r = (unsigned char) ((value >> 24) * scale);
					//g = (unsigned char) (((value >> 16) & 0xff) * scale);
					//b = (unsigned char) (((value >> 8) & 0xff) * scale);
					//a = (unsigned char) ((value & 0xff) * scale);
					break;
				}

				int idx = ((y - y1) * width + (x - x1)) * 4;

				byte_seq[idx++] = r;
				byte_seq[idx++] = g;
				byte_seq[idx++] = b;
				byte_seq[idx++] = a;
			}
		}
	}
	else
	{
    	// convert 2d array to 1d bitmap
		for (uint y = y1; y < y2; y++)
		{
			for (uint x = x1; x < x2; x++)
			{
				unsigned long value = Get (x, y);
				unsigned char r, g, b, a;
				switch (mode)
				{
				case grey_8:
					r = (unsigned char) ((value & 0xff) * scale);
					g = (unsigned char) ((value & 0xff) * scale);
					b = (unsigned char) ((value & 0xff) * scale);
					a = (unsigned char) 255;
					break;
				case rgba_8:
				default:
					r = (unsigned char) ((value >> 24) * scale);
					g = (unsigned char) (((value >> 16) & 0xff) * scale);
					b = (unsigned char) (((value >> 8) & 0xff) * scale);
					a = (unsigned char) ((value & 0xff) * scale);
					break;
				}

				byte_seq[((y2-y) * width + (x - x1)) * 4 + 0] = r;
				byte_seq[((y2-y) * width + (x - x1)) * 4 + 1] = g;
				byte_seq[((y2-y) * width + (x - x1)) * 4 + 2] = b;
				byte_seq[((y2-y) * width + (x - x1)) * 4 + 3] = a;
			}
		}
	}

	return byte_seq;
}

// ===================================================================
// BuildShortSeq
/// @brief 		Convert a 2D image to a 1D sequence of unsigned shorts
///
/// This is in preparation for writing the image out.
//
/// @param	 x1,y1,x2,y2	The bounding points, inclusive
/// @param	 scale          A scaling factor which is applied to each point
/// @return  A pointer to a linear bitmap array
// ===================================================================
unsigned short *Image::BuildShortSeq (uint x1, uint y1, uint x2, uint y2,
	float scale)
{
	uint width = x2 - x1;
	uint height = y2 - y1;
	uint length = width * height;

	unsigned short *short_seq = new unsigned short[length];

	if (short_seq == NULL)
		return NULL;

	if (origin == origin_top)
	{
    	// convert 2d array to 1d bitmap
		for (uint y = y1; y < y2; y++)
		{
			for (uint x = x1; x < x2; x++)
			{
				unsigned long value = Get (x, y);
				short_seq[(y - y1) * width + (x - x1)] = 
					(unsigned short) (value * scale);
			}
		}
	}
	else
	{
    	// convert 2d array to 1d bitmap
		for (uint y = y1; y < y2; y++)
		{
			for (uint x = x1; x < x2; x++)
			{
				unsigned long value = Get (x, y);
				short_seq[(y2 - y) * width + (x - x1)] = 
					(unsigned short) (value * scale);
			}
		}
	}

	return short_seq;
}

// ===================================================================
//  Write
/// @brief Save an entire image to a file
///
/// @param filename		The output filename
/// @param scale		a scaling factor to apply to each point
// ===================================================================

void Image::Write (const char *filename, float scale)
{
	Write (filename, 0, 0, size_x, size_y, scale);
}


// ==========================================================
//  Write
/// @brief Save a region of the image to a file
///
/// @param filename		the basename (no extension) of the file to save
/// @param x1,y1,x2,y2 	the boundaries of the region to save (inclusive)
/// @param scale		a scaling factor to apply to each point
// ==========================================================

void Image::Write (const char *filename, uint x1, uint y1, uint x2, uint y2,
	float scale)
{
	uint width = x2 - x1;
	uint height = y2 - y1;

	unlink (filename);

	unsigned char *byte_seq;
	unsigned short *short_seq;

	// bind image attributes and save
	switch (mode)
	{
	case rgba_8:
	case grey_8:
		byte_seq = BuildByteSeq (x1, y1, x2, y2);

		if (byte_seq == NULL)
		{
			Logger::Instance().Log ("Cannot allocate bitmap for index\n");
			exit (1);
		}
        stbi_write_png(filename, width, height, 4, byte_seq, 0);
        //stbi_write_jpg(filename, width, height, 4, byte_seq, 100);
		delete [] byte_seq;
		break;
	default:
		Logger::Instance().Log ("unsupported mode in image write");
		exit (1);
	}
}

// ==========================================================
// Load
/// \brief		Load an image from a file
/// @param filename    the name of the file to load
// ==========================================================
void Image::Load (const char *filename)
{
    int x,y,n;
    unsigned char *data = stbi_load(filename, &x, &y, &n, 0);
    size_x = x;
    size_y = y;

	allocate (size_x, size_y);

    for (unsigned int y = 0; y < size_y; y++)
    {
        for (unsigned int x = 0; x < size_x; x++)
        {
            unsigned char red = data[(y * size_x + x) * 4 + 0];
            unsigned char green = data[(y * size_x + x) * 4 + 1];
            unsigned char blue = data[(y * size_x + x) * 4 + 2];
            unsigned char alpha = data[(y * size_x + x) * 4 + 3];

            map[x][y] = (red << 24) | (green << 16) | (blue << 8) | alpha;
        }
    }

    stbi_image_free(data);
}

string Image::getExtension ()
{
	switch (format)
	{
	case FORMAT_TGA:
		return string(".tga");
	case FORMAT_PNG:
		return string(".png");
	case FORMAT_JPG:
		return string(".jpg");
	default:
		return string (".unknown");
	}
}

// ==========================================================
// write_page -- write out one page of the image data
// ==========================================================
void Image::write_page (int page_x, int page_y)
{
	int x1 = page_x * page_size;
	int x2 = x1 + page_size;
	int y1 = page_y * page_size;
	int y2 = y1 + page_size;

	ostringstream filename;
	string extension = getExtension();

	filename << name << page_y << "." << page_x << extension;
	Write (filename.str().c_str(), x1, y1, x2, y2);
}

// ==========================================================
// SplitImage -- write the image out as a series of pages
// ==========================================================
void Image::SplitImage (int size)
{
	page_size = size;
	int num_x_pages = (size_x + page_size - 1) / page_size;
	int num_y_pages = (size_y + page_size - 1) / page_size;


	for (int page_x = 0; page_x < num_x_pages; page_x++)
	{
		for (int page_y = 0; page_y < num_y_pages; page_y++)
		{
			write_page (page_x, page_y);
        }
    }
}