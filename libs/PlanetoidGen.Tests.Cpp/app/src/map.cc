#include "action.h"
#include "map.h"
#include "logger.h"
#include "params.h"
#include "pointset.h"

Map::Map (uint x, uint y) : Image (x, y)
{
	Params& params = Params::Instance();

	num_points = (int) (x * y * (params.coverage/100.0));
	boundary.setSize(x, y);
}

Map::Map (std::string filename) : Image (filename.c_str())
{
	Params& params = Params::Instance();
	int x = params.x_size / 2;
	int y = params.y_size / 2;
	num_points = (int) (x * y * (params.coverage/100.0));
	boundary.setSize(x, y);
}

Map::Map () : Image ()
{
	Params& params = Params::Instance();
	int x = params.x_size;
	int y = params.y_size;

	num_points = (int) (x * y * (params.coverage/100.0));
	boundary.setSize(x, y);
}

Map::~Map ()
{
}

// ==========================================================
// Clear -- reset the map's contents to 0's
// ==========================================================

void Map::Clear ()
{
	uint x_size = GetXSize ();
	uint y_size = GetYSize ();

	for (uint j = 0; j < y_size; j++)
		for (uint i = 0; i < x_size; i++)
			Set (i, j, 0);
}

// ==========================================================
// determine if a point has been raised above water
// ==========================================================

bool Map::is_set (uint x, uint y)
{
	long height = Image::Get(x, y);
	return (height > 0);
}

void Map::Set (uint x, uint y, unsigned long value)
{
	Image::Set(x, y, value);
	boundary.insert(x, y);
}

void Map::generate_mask ()
{
	Action *a;
    int x = GetXSize() / 2;
    int y = GetYSize() / 2;
    int dir = rand() % 8;
    Point seed(x, y);
	Params& params = Params::Instance();

	generated = 0;

	// the 4 is to adjust for the halving above
	num_points = (int) (4 * x * y * (params.coverage/100.0));

	a = new Action (this, dir, seed, num_points);

	a -> generate ();
	delete a;

	Logger::Instance().Log ("mask completed\n");
}


// ==========================================================
// Peephole -- examine small areas of the map and fill in holes
// ==========================================================
void Map::Peephole (int level)
{
	int x_size = GetXSize ();
	int y_size = GetYSize ();

    for (int i = 0; i < x_size; i++)
        for (int j = 0; j < y_size; j++)
        {
            if (Get (i, j) != 0)
                continue;

            if (is_surrounded (i, j, level))
                Set (i, j, 1);
        }
}

// ==========================================================
// is_surrounded
// ==========================================================
bool Map::is_surrounded (unsigned int x, unsigned int y, int level)
{
    bool debug = false;

    // 2*level+1 = side length

    for (int i = 0; i < 2*level+1; i++)
    {
        // top edge
        if (debug)
        {
            Logger::Instance().Log ("  (%d,%d) = %d\n", x - level + i, y-level,
                Get (x - level + i, y - level));
            Logger::Instance().Log ("  (%d,%d) = %d\n", x - level + i, y+level,
                Get (x - level + i, y + level));
            Logger::Instance().Log ("  (%d,%d) = %d\n", x - level, y-level+i,
                Get (x-level, y-level+i));
            Logger::Instance().Log ("  (%d,%d) = %d\n", x + level, y-level+i,
                Get (x+level, y-level+i));
        }

        if (Get (x - level + i, y - level) == 0)
            return false;
        // bottom edge
        if (Get (x - level + i, y + level) == 0)
            return false;
        // left edge
        if (Get (x - level, y - level + i) == 0)
            return false;
        // right edge
        if (Get (x + level, y - level + i) == 0)
            return false;
    }

    if (debug)
        Logger::Instance().Log ("surrounded!\n");

    return true;
}

bool
Map::is_surrounded(Point& p)
{
	for (int dir = 0; dir < 8; dir++)
	{
		Point n;
		if (neighbor(p, n, dir))
		{
			if (! is_set (n.x, n.y))
			{
				return false;
			}
		}
	}

	return true;
}
// =======================================================
//  num_free_points
/// @brief Determine the number of surrounding points which are unset
///
/// This is a test to determine of a point is on the boundary, and a
/// measure of how soon the point will be removed from the boundary.
///
/// @param p		The point to examine
/// @param debug	A flag to enable debug logging of the scan
/// @return			The number of neighboring points which are unassigned
// =======================================================
int Map::num_free_points (Point& p)
{
	int count = 0;
	uint x = p.x;
	uint y = p.y;

	// any adjacent 0 point makes this map point 'active'
	for (int i = -1; i < 2; i++)
	{
		for (int j = -1; j < 2; j++)
		{
			// center point does not count
			if ((i == 0) && (j == 0))
			{
				continue;
			}

			if (in_range (x + i, y + j))
			{
				unsigned long value = Get (x+i, y+j);
				if (value == 0)
				{
					count++;
				}
			}
		}
	}

	return count;
}

void
Map::removeFromBoundary (Point& p)
{
	boundary.remove(p.x, p.y);
}

// ===================================================================
// Return a random point on the land/water boundary
// ===================================================================

bool
Map::randomBoundaryPoint(Point &p)
{
	Point bound;

	while (boundary.random_member(bound))
	{
		if (num_free_points(bound) == 0)
		{
			removeFromBoundary (bound);
			continue;
		}

		// is really on the boundary
		p.x = bound.x;
		p.y = bound.y;
		return true;
	}

	return false;
}

// ===================================================================
// Locate a point on land (elevation above 0).
// ===================================================================

bool
Map::randomPointOnMask (Point& point)
{
	bool found = false;

	while (! found)
	{
		int x = rand() % GetXSize();
		int y = rand() % GetYSize();

		if (Get(x, y) > 0)
		{
			point.x = x;
			point.y = y;
			return true;
		}
	}

	// not reached
	return false;
}

// ===================================================================
// Return the coordinates of the neighbor to (x,y) in the specified direction
//
// If the neighboring point is on the map, updates (neighbor_x, neighbor_y) and returns true.
// Otherwise, returns false.
// ===================================================================

bool Map::neighbor (Point& seed, Point& neighbor, int direction)
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
// ==========================================================
// Smooth the entire map
// ==========================================================
void Map::Smooth (int walksize)
{
	int x_size = GetXSize ();
	int y_size = GetYSize ();

    for (int i = 0; i < x_size; i++)
	{
        for (int j= 0; j < y_size; j++)
        {
            smooth_walk (i, j, walksize);
        }
	}
}

// ==========================================================
// smooth_walk -- random walk while smoothing
// ==========================================================
void Map::smooth_walk (uint x, uint y, int steps)
{
    bool move_made = false;
    int count;

    for (int i = 0; i < steps; i++)
    {
        smooth_cell (x, y);

        // perform a random step
        move_made = false;

        count = 0;

        while (! move_made)
        {
            if (count++ == 20)
                return;
            switch (rand() % 4)
            {
                case 0:
                    if (in_range (x+1, y))
                    {
                        x++;
                        move_made = true;
                    }
                    break;
                case 1:
                    if (in_range (x-1, y))
                    {
                        x--;
                        move_made = true;
                    }
                    break;
                case 2:
                    if (in_range (x, y+1))
                    {
                        y++;
                        move_made = true;
                    }
                    break;
                case 3:
                    if (in_range (x, y-1))
                    {
                        y--;
                        move_made = true;
                    }
                    break;
            }
        }
    }
}

// ==========================================================
// ==========================================================
void Map::smooth_cell (uint x, uint y)
{
    smooth_plus (x, y);
}

// ==========================================================
// ==========================================================
void Map::smooth_plus (uint x, uint y)
{
    long sum = 0;
    int live = 0;

    if (! in_range (x, y))
        return;

    sum += 3 * Get (x, y);
    live = 3;

    if (in_range(x+1, y))
    {
        sum += Get (x+1, y);
        live++;
    }

    if (in_range (x-1, y))
    {
        sum += Get (x-1, y);
        live++;
    }

    if (in_range (x, y+1))
    {
        sum += Get (x, y+1);
        live++;
    }

    if (in_range (x, y-1))
    {
        sum += Get (x, y-1);
       live++;
    }

    if (in_range (x+2, y))
    {
        sum += Get (x+2, y);
        live++;
    }

    if (in_range (x-2, y))
    {
        sum += Get (x-2, y);
        live++;
    }

    if (in_range (x, y+2))
    {
        sum += Get (x, y+2);
        live++;
    }

    if (in_range (x, y-2))
    {
        sum += Get (x, y-2);
        live++;
    }

    Set (x, y, sum / live);
}

// =======================================================
//  on_boundary
/// @brief Determine if a point is in the boundary set
///
/// Being in the set is different from actually being on the boundary,
/// as newly surrounded points are still in the set until a scan is made
/// to remove them.
///
/// @param x,y	The coordinates of the point to examine
/// @return True if the point is found in the boundary set, false otherwise
// =======================================================
bool Map::on_boundary (int x, int y)
{
	return boundary.in_set (x, y);
}

// =======================================================
// Boundary iterator accessor.
//
// Loads the next boundary point into p.  Returns true if a
// point was loaded, false when the iterator reaches the end of
// the boundary set.
// =======================================================

bool Map::nextBoundaryPoint(Point &point)
{
	return boundary.Iterate_Next(point);
}