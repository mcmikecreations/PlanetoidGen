#include "Widener.h"
#include "executive.h"
#include "math.h"
#include "SmoothAgent.h"
#include "logger.h"

using namespace std;

bool logPlow = false;
WidenerOp::WidenerOp (TerrainOp& op, int w, int dir, bool doSmooth) : 
	  subOp(op), width(w), direction(dir), smooth(doSmooth)	
{
	decay_elev = 0;
	prev_dir = -1;
}

void WidenerOp::Execute(Point &location)
{
	if ((location.x == 133) && (direction == 3))
	{
		logPlow = true;
	}
	else
	{
		logPlow = false;
	}
	logPlow = false;

	processLocation(location, direction);
}

void WidenerOp::setPrevious(Point& p, int dir)
{
	previous = p;
	prev_dir = dir;

}

// ===================================================================
// Determine if a given direction is on one of the diagonals.
//
// Diagonals need additional filling to avoid gaps.
// ===================================================================

bool WidenerOp::isDiagonal (int dir)
{
    if ((dir == DIR_UL) || (dir == DIR_LL) || (dir == DIR_LR) ||
        (dir == DIR_UR))
    {
        return true;
    }

    return false;
}

// ===================================================================
// Continue to plow along the path
//
// If the path direction has changed, additional filling will be needed
// to avoid gaps.  The same is true if the path is currently moving in
// a diagonal line.  Otherwise plow ahead.
// ===================================================================

void WidenerOp::processLocation (Point& location, int dir)
{
    if (dir != prev_dir)
    {
		if (logPlow)
		{
			Logger::Instance().Log ("changing direction from %d to %d at (%d,%d)\n", prev_dir, dir, location.x, location.y);
		}

        changeDirection (location.x, location.y, dir);
        return;
    }

    int sliceDir = (dir + 2) % 8;

    processSlice (location.x, location.y, sliceDir);

    if (isDiagonal (sliceDir))
    {
		if (logPlow)
		{
			Logger::Instance().Log ("sliceDir is seen to be on a diagonal, filling\n");
		}

        processSlice (previous.x, location.y, sliceDir);
        processSlice (location.x, previous.y, sliceDir);
    }
	else if (logPlow)
	{
		Logger::Instance().Log ("sliceDir %d is not on a diagonal, not filling\n");
	}

	previous = location;
}

// ===================================================================
// Determine the dx/dy which when applied to a point moves it in a direction.
// ===================================================================

void WidenerOp::convertDirection(int dir, int& dx, int& dy)
{
	switch (dir)
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
			Logger::Instance().Log ("invalid direction passed to Widener::convertDirection\n");
	}

	return;
}

// ===================================================================
// Apply the subop operator to each point in the slice, accounting for
// additional fill points as needed.
// ===================================================================

void WidenerOp::processSlice (int x, int y, int dir)
{
	int sliceWidth = width;
	int dx;
	int dy;

//	Logger::Instance().Log ("widener starting at (%d,%d) dir %d\n", x, y, dir);

	if (isDiagonal(dir))
	{
		sliceWidth -= 2;
	}

	if (logPlow)
	{
		Logger::Instance().Log ("processSlice %d,%d dir=%d\n", x, y, dir);
	}

  convertDirection (dir, dx, dy);

  for (int i = 0; i < sliceWidth; i++)
  {
    int offset_x = (i - sliceWidth/2) * dx;
    int offset_y = (i - sliceWidth/2) * dy;
	int cl_distance = abs((sliceWidth/2) - i);

	Point p(x + offset_x, y + offset_y);

	if (logPlow)
	{
		Logger::Instance().Log ("point on slice is (%d,%d)\n", p.x, p.y);
		Logger::Instance().Log ("center point (%d,%d), clDistance = %d\n", x, y, cl_distance);
	}

	if (! Executive::Instance().onMap(p))
	{
		if (logPlow)
		{
			Logger::Instance().Log ("skipping point which is off the map\n");
		}

		continue;
	}

	// 	int delta = rand() % 50 + 100;			// the original MountainAgent decay

	if (decay_elev)
	{
		int delta = decay_elev * cl_distance;
		//Logger::Instance().Log ("decaying elev by %d\n", delta);
		subOp.setDelta(delta);
	}

	leftTail.x = x - (sliceWidth/2 * dx);
	leftTail.y = y - (sliceWidth/2 * dy);
	rightTail.x = x + (sliceWidth/2 * dx);
	rightTail.x = x + (sliceWidth/2 * dy);

	Executive::Instance().operatePoint(p, subOp);
  }
}

// ===================================================================
// Determine the next point in the path
// ===================================================================

void WidenerOp::step (int& x, int& y, int dir)
{
    int dx, dy;

    convertDirection (dir, dx, dy);
    x = x + dx;
    y = y + dy;
}

// ===================================================================
// Handle the case where a path's direction changes.
//
// Additional fills are needed to avoid gaps.  The best fill algorithm
// found was to continue the path forward for half the path length, and then
// return to the previous point and execute the turn.
// ===================================================================

void WidenerOp::changeDirection (int x, int y, int dir)
{
  int px = previous.x;
  int py = previous.y;

  int sliceDir = (prev_dir + 2) % 8;

  if (logPlow)
  {
	  Logger::Instance().Log ("previous slices would be moving in direction %d\n", sliceDir);
  }

  int x2 = px;
  int y2 = py;
  for (int i = 0; i < width/2; i++)
  {
    processSlice (x2, y2, sliceDir);

    if (isDiagonal (sliceDir))
    {
      processSlice (px, y2, sliceDir);
      processSlice (x2, py, sliceDir);
    }

    px = x2;
    py = y2;

    step (x2, y2, prev_dir);
  }

  sliceDir = (dir + 2) % 8;
  processSlice (x, y, sliceDir);

  prev_dir = dir;
  previous.x = x;
  previous.y = y;
}

void WidenerOp::getTailPoints (Point& left, Point& right)
{
	left = leftTail;
	right = rightTail;
}
