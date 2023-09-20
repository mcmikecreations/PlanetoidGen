#include "action.h"
#include "logger.h"
#include <sstream>
#include "pointset.h"
#include <stdlib.h>
#include "params.h"
#include "executive.h"

const int BAD_SCORE = -10000000;

int Action::count = 0;

// =======================================================
/// @brief Action constructor
///
/// @param m	The map being worked on
/// @param dir	The preferred direction for this action to head in
/// @param seed	A seed point (where the action begins)
/// @param sz	The number of points (size) the action is to generate
// =======================================================
Action::Action (Map *m, int dir, Point& seed, int sz, int _parent)
{
	long dist;

	id = count++;
	parent = _parent;

	mask = m;
	action_size = sz;

	history_size = 0;
	value = 50000;

	if (Dist_to_Edge (seed) < 60)
	{
		direction = towardsCenter(seed);
	}
	else
	{
		direction = dir;
	}

	bset.setSize(mask->GetXSize(), mask->GetYSize());

	// Logger::Instance().Log ("New Action %d (%d, %d)\n", id, seed.x, seed.y);
	// Logger::Instance().Log ("Size = %d, Dir = %d\n", action_size, dir);

	// hopefully the seed point is an active point, but in case it is not, search the action's ray 
	// used to check for 0 elevation, but points on the boundary should have been elevated already
	if (!is_active(seed))
	{
		Point point;
		// Logger::Instance().Log ("action %d, initial seed (%d,%d) is not active, searching ray\n", id, seed.x, seed.y);
		if (find_active_on_ray (seed, point))
		{
			seed_pt.x = point.x;
			seed_pt.y = point.y;
			// Logger::Instance().Log ("found new seed point %d,%d\n", point.x, point.y);
		}
		else
		{
			seed_pt.x = seed.x;
			seed_pt.y = seed.y;
		}
		
	}
	else
	{
		seed_pt.x = seed.x;
		seed_pt.y = seed.y;
	}

//	Logger::Instance().Log ("Action %d starting at (%d,%d)\n", id, seed_pt.x, seed_pt.y);

	if (is_active (seed_pt))
	{
		// mask->Set(seed_pt.x, seed_pt.y, value);
		Add (seed_pt);
	}

	// dist = Dist_to_Edge (seed_pt) - 4;

	int dir1 = rand() % 8;			// dir of attractor
	int dir2 = dir1;					// dir of repulsor

	dist = rand() % mask->GetXSize();
	StepDir(seed_pt, attractor, dir1, dist);
	// Logger::Instance().Log ("action %d set attractor (%d,%d)\n", id, attractor.x, attractor.y);

	while (dir1 == dir2)
	{
		dir2 = rand() % 8;
	}
	dist = rand() % mask->GetXSize();
	StepDir(seed_pt, repulsor, dir2, dist);
	// Logger::Instance().Log ("action %d set repulsor (%d,%d)\n", id, repulsor.x, repulsor.y);

}

Action::~Action ()
{
}


// =======================================================
//  Add
/// @brief	Add a point to the boundary set
/// @note The boundary set owns the contained points storage
///
/// @param p	The point to add to the boundary
// =======================================================
void Action::Add (Point& p)
{
	if (Dist_to_Edge(p) < 10)
	{
		return;
	}

	bset.insert (p.x, p.y);

	if (! mask->is_surrounded(p))
	{
		mask->Set (p.x, p.y, value);
	}
}

// =======================================================
// Remove
/// @brief Remove a point from the boundary set
///
/// @param x,y	The coordinates of the point to remove
// =======================================================

void Action::Remove (Point& p)
{
	bset.remove (p.x, p.y);
}


// =======================================================
// Remove_Invalid_Neighbors
/// @brief		Remove newly surrounded points from the boundary
///
/// The boundary set should only contain points which are not surrounded.
/// As points are filled points which were in the boundary set begin to be
/// surrounded.  Scan neighboring points to see if any need to be
/// removed.
///
/// @param p	A point whose neighbors need to be examined
// =======================================================
void Action::Remove_Invalid_Neighbors (Point& p)
{
	for (int dir = 0; dir < 8; dir++)
	{
		Point neighbor;
		
		if (! StepDir (p, neighbor, dir))
		{
			continue;
		}

		if (mask->is_surrounded(neighbor))
		{
			Remove (neighbor);
			mask->removeFromBoundary (neighbor);
		}
#if LOCAL_BOUNDARY
		if ((!is_active (neighbor)) && 
			 (on_boundary(neighbor.x, neighbor.y)))
		{
			Remove (neighbor.x, neighbor.y);
		}
#endif
	}
}

// =======================================================
//  dump_active
/// @brief	Display the boundary set
///
/// At one point in time, points in the boundary set were labeled
/// "active", as in still able to be expanded.  This method logs
/// the contents of the boundary set using the Logger singleton.

// =======================================================
void Action::dump_active ()
{
	Point p;
	int count = 0;

	Logger::Instance().Log ("Active Boundary Points:\n");

	mask->resetBoundaryIterator();

	while (mask->nextBoundaryPoint (p))
	{
		std::ostringstream msg;
		msg << "   [" << count << "]:  (" << p.x << "," << p.y;
		msg << ") ";

		msg << mask->num_free_points (p) << " neighbors free ";

		if (! is_active (p))
			msg << "(inactive)";
		else
			msg << "(active)";

		Logger::Instance().Log (msg.str());
	}
}

// =======================================================
//  Dist_to_Edge
/// @brief Return the Manhattan distance to the closest map edge
///
/// @param p	The point to measure from
/// @return		The Manhattan distance from p to the closest edge
// =======================================================
long Action::Dist_to_Edge (Point& p)
{
	long dist;

    long d1 = p.x;
    long d2 = mask -> GetXSize() - p.x;

    long d3 = p.y;
    long d4 = mask -> GetYSize() - p.y;

    dist = d1;
    if (d2 < dist)
        dist = d2;
    if (d3 < dist)
        dist = d3;
    if (d4 < dist)
        dist = d4;

    return dist;
}

// =======================================================
//  find_active_on_ray 
/// @brief Search along a ray for a point on the boundary
///
/// Recall that boundary points have at least one adjacent point which has not been set
/// (ie a 0 elevation).
///
/// @param src The source point to being searching from
/// @param result The located point (if found)
/// @return The first point found on the boundary, or false if none found
// =======================================================
bool Action::find_active_on_ray (Point& src, Point& result)
{
	int x = src.x;
	int y = src.y;

	// walk p along ray until active point seen, or map edge hit
	Point p(src);

	while (mask -> in_range (p.x, p.y))
	{
		Point next;

		// p always a point, but may be off map
		StepDir (p, next, direction);

		if (is_active (next))
		{
			result.x = next.x;
			result.y = next.y;
			return true;
		}

		// Logger::Instance().Log ("action %d searching ray, skipping inactive point\n", id);

		p.x = next.x;
		p.y = next.y;
	}

	Logger::Instance().Log ("action %d failed to find an active point on ray\n", id);
	return false;
}

// =======================================================
//  is_active
/// @brief Determine if the point is still on the boundary
///
/// A point is considered on the boundary if there is at least one
/// neighboring point which has not been filled in.  A scan of neighboring
/// points is made.
///
/// @param p		The point to examine
/// @param debug	A flag to enable debug logging of the scan
/// @return			True if p is on the boundary, false otherwise
// =======================================================
bool Action::is_active (Point& p, bool debug)
{
	return (mask->num_free_points (p) > 0);
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
bool Action::on_boundary (int x, int y)
{
	return bset.in_set (x, y);
}

// =======================================================
// Log the map around a specified point
// =======================================================
void Action::Display_Region (Point& p, int range)
{
	int x = p.x;
	int y = p.y;

	std::ostringstream msg;

	msg << "Map from x = " << (x - range) << "-" << (x + range - 1);
	msg << "  y = " << (y - range) << "-" << (y + range - 1);
	Logger::Instance().Log (msg.str());

	for (int i = (x - range); i < (x + range); i++)
	{
		msg.str("");
		for (int j = (y - range); j < (y + range); j++)
		{
			if (! mask -> in_range (i, j))
			{
				msg << " X ";
			}
			else if (mask -> Get (i, j) == 0)
			{
				msg << " . ";
			}
			else
			{
				msg << " + ";
			}
		}

		msg << std::endl;
		Logger::Instance().Log (msg.str());
	}

	msg.str("");
	msg << "Key:  X=off map, . = unset, + = set";
	Logger::Instance().Log (msg.str());
	msg.str("");
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

bool Action::StepDir (Point& src, Point& dst, int direction, int delta)
{
#if 0
	int x = src.x;
	int y = src.y;

	switch (direction)
	{
		case DIR_UL:
			x -= delta;
			y -= delta;
			break;
		case DIR_UP:
			y -= delta;
			break;
		case DIR_UR:
			x += delta;
			y -= delta;
			break;
		case DIR_LEFT:
			x -= delta;
			break;
		case DIR_RIGHT:
			x += delta;
			break;
		case DIR_LL:
			x -= delta;
			y += delta;
			break;
		case DIR_DOWN:
			y += delta;
			break;
		case DIR_LR:
			x += delta;
			y += delta;
			break;
	}

	dst.x = x;
	dst.y = y;
#endif
	return Executive::Instance().StepDir(src, dst, direction, delta);
}

// =======================================================
//  random_active
/// @brief Return a random, active boundary point
///
/// A point is returned which is either in the boundary set (and chosen
/// randomly, or in the case of an empty boundary set the first active
/// point on a random ray from the Action seed point.
///
/// @param point The returned point (only modified if a valid point found)
/// @return true if a point was found and assigned to "point", false otherwise
// =======================================================
bool Action::random_active (Point &point)
{
	bool done = false;
	Point p;

	if (! bset.random_member(p))
	{
//		Logger::Instance().Log ("action %d appears to have an empty boundary set, searching ray from seed\n", id);
		return find_active_on_ray (seed_pt, point);
	}

	// point was found, and is valid
	// Logger::Instance().Log ("random active (%d,%d): %d free points\n", p.x, p.y, mask->num_free_points(p));

	point.x = p.x;
	point.y = p.y;
	return true;
}

// =======================================================
//  Hist_Point
/// @brief Return the oldest point in the history
///
///
/// @return The oldest point in the history, or NULL if the history is empty
// =======================================================
bool Action::Hist_Point (Point& p)
{
	if (history_size > 0)
	{
		p.x = history[history_size - 1].x;
		p.y = history[history_size - 1].y;
		return true;
	}
	else
	{
		return false;
	}
}

// =======================================================
//  DistSqr
/// @brief Return the squared distance between two points
///
/// Squared distance is used rather than pure distance since it is
/// quicker to calculate, and still useful for ordering points.
///
/// @param a,b	The points to measure the distance between
/// @return The square of the Euclidian distance between the points
// =======================================================
long Action::DistSqr (Point& a, Point& b)
{
	return ((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
}

// =======================================================
//  Score
/// @brief  Evaluate a candidate point (calculate a score based on criteria)
//
/// Criteria:
///		(+) distance to attractor
///		(-) distance to repulsor
///     (-) distance to map edge
///     (+) historical trend fitting
///
/// @param p The point to evaluate
/// @return A numerical score describing how well the point met the criteria
// =======================================================
long Action::Score (Point& p)
{
	long d_attr = DistSqr (p, attractor) / 10;
	long d_repl = DistSqr (p, repulsor) / 10;
	long d_map = Dist_to_Edge (p);

	Point h;
	long d_hist;

	if (Dist_to_Edge(p) < 10)
	{
		return -1000;
	}

	if (! in_range(p))
	{
		return -1000;
	}

	if (Hist_Point (h))
	{
		d_hist = DistSqr (p, h) / 10;
	}
	else
	{
		d_hist = 0;
	}

	std::ostringstream msg;

#if LOG_SCORING
	msg << "Point (" << p->x << "," << p->y << "): ";
	msg << "d_attr=" << d_attr << " d_hist=" << d_hist;
	msg << " d_repl=" << d_repl << " d_map=" << d_map;
	Logger::Instance().Log (msg.str());
	Logger::Instance().Log ("Total = %d\n", 
		d_attr + d_hist - (d_repl + d_map));
#endif

	// rough scoring function
	return (d_attr + d_hist + (3 * d_map) - d_repl);
}

// =======================================================
//  in_range
/// @brief Determine if a point is on the map
///
/// @param p	The point to evaluate
/// @return		True if the point is on the map, false otherwise
// =======================================================
bool Action::in_range (Point& p)
{
	return mask -> in_range (p.x, p.y);
}

// =======================================================
//  PlotPixels
/// @brief Fill points adjacent to a random set of boundary points
///
/// Each Action object has a localized boundary set (that is, a subset
/// of the true map boundary in one section of the map).  Random unfilled
/// points next to these boundary points are filled in, causing the map to
/// grow.
// =======================================================
void Action::PlotPixels ()
{
	Point p;

	if (! random_active(p))
	{
			Logger::Instance().Log ("random_active found no points\n");
			return;
	}

	for (unsigned int i = 0; i < action_size; i++)
	{
		if (! random_active (p))
		{
			Logger::Instance().Log ("random_active found no points\n");
			return;
		}

		if (! is_active (p))
		{
			Logger::Instance().Log ("aborting PlotPixels since point wasn't marked active\n");
			return;
		}

		expand_pt (p);
	}
}

// =======================================================
//  expand_pt
/// @brief Fill a point adjacent to p
///
/// All neighboring points to p are scored (evaluated using a criteria),
/// and the highest scoring unfilled neighboring point is filled in.  This
/// gives the growth process a localized bias which varies from Action to
/// Action.
///
/// @param p	A boundary point to expand from
// =======================================================
void Action::expand_pt (Point& p)
{
	Point best_neighbor;
	int curr_direction = direction;
	int best_score = BAD_SCORE;

	// Logger::Instance().Log ("action %d expanding point (%d,%d)\n", id, p.x, p.y);

	// score all adjacent points
	for (int dir = 0; dir < 8; dir++)
	{
		Point neighbor;
		int score = BAD_SCORE;

		StepDir (p, neighbor, dir);

		// skip non-blank points
		if (mask -> Get (neighbor.x, neighbor.y) != 0)
		{
			continue;
		}

		score = Score (neighbor);

		if (score > best_score)
		{
			best_score = score;
			best_neighbor.x = neighbor.x;
			best_neighbor.y = neighbor.y;
		}
	}

	if (best_score < 0)
	{
		return;
	}

	// see if any neighbor scored above BAD_SCORE
	if (best_score > BAD_SCORE)
	{
		//if (Dist_to_Edge(best_neighbor) < 10)
		//{
		//	return;
		//}

		mask -> Set (best_neighbor.x, best_neighbor.y, value);

		// only add if there are surrounding pts free, have seen cases
		// where a completely surrounded free pt was being filled
		if (is_active (best_neighbor))
		{
			Add (best_neighbor);
		}

		Remove_Invalid_Neighbors (best_neighbor);
	}
}

// =======================================================
//  generate
/// @brief Generate around 'size' pixels of data in this region of the map
///
/// Actions operate in a local region.  The generate() method causes a set
/// number of points to be added to the map in this region.
// =======================================================
void Action::generate ()
{
	Params& params = Params::Instance();
	unsigned int action_size_limit = (rand() % (params.action_size_max - params.action_size_min)) + params.action_size_min;

	// once size gets small, just generate raw pixels
	if (action_size <= action_size_limit)
	{
		PlotPixels ();
		return;
	}

	// otherwise, subdivide
	for (int i = 0; i < 2; i++)
	{
		Point p;
		
		if (! random_active (p))
		{
			return;
		}

		int dir = rand() % 8;
		int best_score = BAD_SCORE;
		int best_dir = dir;

		for (int i = 0; i < 3; i++)
		{
			int d = (dir + i - 1);
			d = d % 8;
			if (d < 0)
				d +=8;

			Point dst;
			StepDir(p, dst, d);


			int score = Score(dst);
			if (score > best_score)
			{
				best_dir = d;
				best_score = score;
			}
		}

		Point dst;
		StepDir(p, dst, best_dir);
		if (Dist_to_Edge(dst) < 10)
		{
			return;
		}
//		Logger::Instance().Log ("action %d (from %d) is splitting, attempting to seed from (%d,%d)\n", id, parent, p.x, p.y);
//		Logger::Instance().Log ("action %d is currently generating %d points\n", id, action_size);
		Action *sub = new Action (mask, best_dir, p, action_size/2, id);
		sub -> generate ();
		delete sub;
	}
}

int Action::towardsCenter(Point& p)
{
	Params& params = Params::Instance();

	Point MapCenter(params.x_size / 2, params.y_size / 2);
	return (Executive::Instance().directionFrom(p, MapCenter));
}