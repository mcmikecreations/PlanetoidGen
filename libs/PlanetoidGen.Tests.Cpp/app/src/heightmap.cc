#include "heightmap.h"
#include <math.h>
#include "pointset.h"
#include "logger.h"
#include "params.h"
#include "executive.h"

#include <fstream>
#include <sstream>
#include <iostream>

Heightmap::Heightmap (int x, int y)
	: Image (x, y)
{
	for (int i = 0; i < x; i++)
		for (int j = 0; j < y; j++)
			Set(i,j, 0);
}

Heightmap::~Heightmap ()
{
}


// ===================================================================
// randomize -- generate random heights within a band
//
// 5/12/2008:  I used to allow negative altitudes but that appears
// not to be the case anymore (Image contains only unsigned values)
// ===================================================================

void Heightmap::randomize (unsigned long band_size)
{
	Params& params = Params::Instance();
	//int midpoint =  params.height_limit / 5;
	int midpoint = 3000;
	int half_band = band_size / 2;								// half above and half below mid

	Logger::Instance().Log ("Randomizing\n");
	for (unsigned int i = 0; i < (unsigned int) params.y_size; i++)
	{
		for (unsigned int j = 0; j < (unsigned int) params.x_size; j++)
		{
			Point p(j, i);

			if (Executive::Instance().on_land (p))
			{
				int offset = rand() % band_size;

				// translate the band to half the max height
				unsigned long altitude = (unsigned long) 
					(midpoint + offset - half_band);

				Set (j, i, altitude);
			}
			else
			{
				Set (j, i, 0);
			}
		}
	}
}

// ===================================================================
// Max -- return the highest point on the map
//
// This is useful for scaling.
// ===================================================================
unsigned long Heightmap::Max ()
{
	unsigned long max = 0;

	for (unsigned int i = 0; i < GetXSize(); i++)
	{
		for (unsigned int j = 0; j < GetYSize(); j++)
		{
			unsigned long value = Get (i, j);
			if (value > max)
			{
				max = value;
			}
		}
	}

	return max;
}

// ===================================================================
// Min -- return the lowest point on the map
//
// This is useful for scaling.
// ===================================================================
unsigned long Heightmap::Min ()
{
	unsigned long min = Get(0,0);

	for (unsigned int i = 0; i < GetXSize(); i++)
	{
		for (unsigned int j = 0; j < GetYSize(); j++)
		{
			unsigned long value = Get (i, j);
			if (value < min)
			{
				min = value;
			}
		}
	}

	return min;
}

// ===================================================================
// Return the coordinates of the neighbor to (x,y) in the specified direction
//
// If the neighboring point is on the map, updates (neighbor_x, neighbor_y) and returns true.
// Otherwise, returns false.
// ===================================================================

bool Heightmap::neighbor (Point& seed, Point& neighbor, int direction)
{
	int nx = seed.x;
	int ny = seed.y;

	switch (direction)
	{
		case DIR_RIGHT:
			nx = seed.x + 1;
			break;
		case DIR_LEFT:
			nx = seed.x - 1;
			break;
		case DIR_DOWN:
			ny = seed.y + 1;
			break;
		case DIR_UP:
			ny = seed.y - 1;
			break;
	}

	if (in_range (nx, ny))
	{
		if ((nx != seed.x) || (ny != seed.y))
		{
			// only modify the params when we are returning a valid point
			neighbor.x = nx;
			neighbor.y = ny;
			return true;
		}
	}

	return false;
}

// ===================================================================
// Return a random neighboring point
// ===================================================================
bool Heightmap::random_neighbor (Point& src, Point& neighbor)
{
	int direction = rand() % 4;
	bool done = false;
	int x = src.x;
	int y = src.y;

	while (! done)
	{
		switch (direction)
		{
			case 0:
				if ((x + 1) < (int) GetXSize())
				{
					x++;
				}
				break;
			case 1:
				if (x > 0)
				{
					x--;
				}
				break;
			case 2:
				if ((y + 1) < (int) GetYSize())
				{
					y++;
				}
				break;
			case 3:
				if (y > 0)
				{
					y--;
				}
				break;
		}

		if (in_range (x, y))
		{
			if ((src.x != x) || (src.y != y))
			{
				done = true;
			}
		}

		++direction %= 4;
	}

	neighbor.x = x;
	neighbor.y = y;
	return true;
}

// =======================================================
//  StepDir
/// @brief Return a neighboring point in the specified direction
///
/// A "step" is made in the specified direction, and the point which
/// is landed on is returned.
///
/// @note No guarantee is made that the point is still on the map, or
/// that it is in the boundary set.  Those checks must be made separately.
///
/// @param src			The origin point
/// @param direction	The direction to step
/// @param delta		The size of the step
///
/// @return A point which is the result of the specified step
// =======================================================

bool Heightmap::StepDir (Point& src, Point& dst, int direction, int delta)
{
	int x = src.x;
	int y = src.y;
	int dx, dy;

	switch (direction)
	{
		case DIR_UP:
			dx = 0;
			dy = 1;
			break;
		case DIR_UR:
			dx = 1;
			dy = 1;
			break;
		case DIR_RIGHT:
			dx = 1;
			dy = 0;
			break;
		case DIR_LR:
			dx = 1;
			dy = -1;
			break;
		case DIR_DOWN:
			dx = 0;
			dy = -1;
			break;
		case DIR_LL:
			dx = -1;
			dy = -1;
			break;
		case DIR_LEFT:
			dx = -1;
			dy = 0;
			break;
		case DIR_UL:
			dx = -1;
			dy = 1;
			break;
		default:
			dx = 0;
			dy = 0;
	}

	dst.x = x + dx;
	dst.y = y + dy;
	return true;
}

// ===================================================================
// Scale -- rescale the map so that values are from 0..limit
// ===================================================================

void
Heightmap::Scale (unsigned long limit)
{
	unsigned long max_value = Max ();

	float scaling_factor = (float) limit / (float) max_value;

	Logger::Instance().Log ("scale map to 0-%d.  Max value on map is %d, factor is %f\n", limit, max_value, scaling_factor);

	for (unsigned int i = 0; i < GetXSize(); i++)
	{
		for (unsigned int j = 0; j < GetYSize(); j++)
		{
			unsigned long unscaled = Get(i, j);
			unsigned long scaled = (unsigned long) (scaling_factor * unscaled);

			Set (i, j, scaled);
		}
	}
}
