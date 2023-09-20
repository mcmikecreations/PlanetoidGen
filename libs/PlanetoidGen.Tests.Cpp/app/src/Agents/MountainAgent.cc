#include "MountainAgent.h"
#include "SmoothAgent.h"
#include "Widener.h"

#include "executive.h"
#include "logger.h"
#include <sstream>
#include "params.h"

#include "heightmap.h"	// for direction enum

using namespace std;

int MountainAgent::count = 0;

// ===================================================================
// The MountainAgent runs around trying to raise mountain ranges.  These are
// parallel
// ===================================================================

MountainAgent::MountainAgent (int _tokens)
{
	Params& params = Params::Instance();

	type = MOUNTAIN_AGENT;

	count++;
	id = count;

	ostringstream buf;
	buf << "Mountain Agent #" << id;
	name = buf.str();

	tokens = _tokens;
	runnable = true;
	initializing = true;
	width = params.mountain_width;

	base_direction = rand() % 8;
	current_direction = base_direction;


}

// ===================================================================
// Execute one timestep
// ===================================================================
bool MountainAgent::Execute ()
{
	Params& params = Params::Instance();

	Point nextLocation;
	bool misstep = false;
	int misstep_count;

	if ((tokens == 0) && runnable)
	{
//		Logger::Instance().Log ("%s finishing\n", name.c_str());
		int count = 0;

#if SHOW_PEAKS
		peaks.Reset_Iterator();
		while (peaks.Iterate_Next(nextLocation))
		{
			Logger::Instance().Log (" (%d,%d) ", nextLocation.x, nextLocation.y);
			if (count++ % 10 == 9)
				Logger::Instance().Log ("\n");		
		}
		Logger::Instance().Log ("\n");
#endif
		runnable = false;
		return false;
	}

	if (tokens == 0)
	{
		return false;
	}

	if (initializing)
	{
		initializing = false;
		prev_dir = current_direction;

			if (!Executive::Instance().random_land(location))
			{
				Logger::Instance().Log ("%s cannot be placed on land\n", name.c_str());
				return false;
			}
#if DISTANCE_CHECKS
		do
		{
			if (!Executive::Instance().random_land(location))
			{
				Logger::Instance().Log ("%s cannot be placed on land\n", name.c_str());
				return false;
			}
		} while (Executive::Instance().distanceToCoastline(location) < 3000);
#endif

		previous = location;
		Logger::Instance().Log ("%s starting at (%d,%d), direction = %d\n", name.c_str(), location.x, location.y,
			base_direction);
	}



	tokens--;

	//Logger::Instance().Log ("%s working at %d,%d\n", name.c_str(), location.x, location.y);



	int height = altitude + rand() % variance;
	//Logger::Instance().Log ("%s setting height to %d\n", name.c_str(), height);


	// Peaks is the set of mountain centerline points.  Altitude likely slopes away from these.
	if ((height > params.hill_max_alt) && (Executive::Instance().on_land(location)))
	{
		peaks.insert(location);
	}
	elevateSlice (location, height, width);

	previous = location;
	prev_dir = current_direction;

#if 0
	// smoothing is needed to make the slices blend
	// replacing this with a new roughener operation

	if (rand() % 100 < params.mountain_smooth_prob)
	{
		Executive::Instance().smoothArea(location);
	}
#endif

	//Executive::Instance().smoothArea(location);

#if 0
	// if we are at the end of our push, do some additional smoothing
	if (tokens == 0)
	{
		SmootherOp smootherOp;
		WidenerOp widenerOp2(smootherOp, width, current_direction, true);	
		Executive::Instance().operatePoint(location, widenerOp2);
		return false;
	}
#endif

	// Perform one step in a random walk, attempt to move until a legal move is found
	misstep_count = 0;
	do
	{
		misstep = false;

		if (! Executive::Instance().StepDir(location, nextLocation, current_direction))
		{
			Logger::Instance().Log ("%s failed to perform random walk\n", name.c_str());
			misstep = true;
		}
		else if (! Executive::Instance().on_land(nextLocation))
		{
			misstep = true;
		}
		
		if (misstep_count == 10)
		{
			Logger::Instance().Log ("%s has hit 10 missteps\n", name.c_str());
			return false;
		}

		if (! misstep)
		{
			int height = Executive::Instance().getHeight(nextLocation);
			if (height < 4)
			{
				misstep = true;
			}
		}

		if (misstep_count++ % 4 == 3)
		{
			++base_direction %= 8;
		}

		if (misstep)
		{
			prev_dir = current_direction;
			changeDirection();
		}
		else
		{
			location.x = nextLocation.x;
			location.y = nextLocation.y;
		}
	} while (misstep);

	if (tokens % 7 == 0)
	{
		prev_dir = current_direction;
		changeDirection();	
	}

	 //Logger::Instance().Log ("%s advancing to %d,%d", name.c_str(), location.x, location.y);
	 //if (Executive::Instance().on_land(location))
	 //{
		// Logger::Instance().Log (" on-land\n");
	 //}
	 //else
	 //{
		// Logger::Instance().Log (" NOT on land\n");
	 //}

	return true;
}

// ===================================================================
// change the direction the mountain agent is moving
//
// Will zig-zag around the original direction.  If the agent runs into
// a barrier, the Execute method will modify the original direction in
// an attempt to escape.  So this zig-zagging will rotate along with the
// new path.
// ===================================================================
void MountainAgent::changeDirection()
{
	if (current_direction == base_direction)
	{
		if (rand() % 2 == 1)
		{
			++current_direction;
		}
		else
		{
			--current_direction;
		}
	}
	else if (current_direction == ((base_direction + 1) % 8))
	{
		--current_direction;
	}
	else
	{
		++current_direction;
	}

	if (current_direction == 8)
	{
		current_direction = 0;
	}
	if (current_direction == -1)
	{
		current_direction = 7;
	}
}

// ===================================================================
// Elevates a "slice" of the mountain.
//
// Rather than elevating a point at the mountain peak, or a block around
// the peak, this is an experiment in elevating points to the left and the
// right of the peak.  The goal is to make the mountain wider.
// ===================================================================

void MountainAgent::elevateSlice (Point& location, int altitude, int width)
{
	Params& params = Params::Instance();

	SetHeightOp altitudeOp(altitude);
	WidenerOp elevateOp(altitudeOp, width, current_direction, false);

	// int decay = rand() % params.mountain_slope_max + params.mountain_slope_min;
	int decay = altitude / 100;
	decay += (int) (2.5 * decay);

	if (prev_dir == -1)
	{
		prev_dir = 0;
	}

	elevateOp.setDecay (decay);						// per point away from centerline
	elevateOp.setPrevious(previous, prev_dir);

	SmootherOp smoother;
	WidenerOp smootherOp(smoother, width, current_direction, false);
	smootherOp.setPrevious(previous, prev_dir);
	FixPointOp fixpoint;
	WidenerOp fixpointWidener(fixpoint, width, current_direction, false);
	fixpointWidener.setPrevious(previous, prev_dir);

	// these are calls to the widener, to apply the terrain op to each point on the slice
	Executive::Instance().operatePoint(location, elevateOp);

	int foothill_token = (100 - params.foothill_freq);

	if ((tokens % foothill_token == (foothill_token - 1)) && (altitude > 18000))
	{
		Point MapCenter(params.x_size / 2, params.y_size / 2);

		// record the mountain base points
		Point left, right;
		elevateOp.getTailPoints(left, right);
		int len = rand() % (params.foothill_max_length - params.foothill_min_length) + params.foothill_min_length;

		int leftDist = Executive::Instance().distanceSq(left, MapCenter);
		int rightDist = Executive::Instance().distanceSq(right, MapCenter);
		if (leftDist < rightDist)
		{
			int dir = Executive::Instance().directionFrom(location, left);

			MountainAgent *agent = new MountainAgent(len);
			agent->setAltitudePreferences(altitude / 2, variance/10);
			agent->setDirection(dir);
			Executive::Instance().StepDir(left, left, dir, 10);
			agent->setLocation(left);
			agent->setWidth(width - 10);
			Executive::Instance().addAgent(agent);
		}
		else
		{
			int dir = Executive::Instance().directionFrom(location, right);

			MountainAgent *agent = new MountainAgent(len);
			agent->setAltitudePreferences(altitude / 2, variance/10);
			agent->setDirection(dir);
			Executive::Instance().StepDir(right, right, dir, 10);
			agent->setLocation(right);
			agent->setWidth(width - 10);
			Executive::Instance().addAgent(agent);
		}
	}

	Executive::Instance().operatePoint(location, smootherOp);

	RoughenerOp roughener;
	roughener.setProb(params.mountain_rough_prob);
	roughener.setVariance(params.mountain_rough_var);
	WidenerOp roughWidener(roughener, width, current_direction, false);
	roughWidener.setPrevious(previous, prev_dir);

	Executive::Instance().operatePoint(location, smootherOp);
	Executive::Instance().operatePoint(location, roughWidener);

	// fixing points this early gives really bad results
//	Executive::Instance().operatePoint(location, fixpointWidener);

}

// ===================================================================
// return a random point on the mountain
// ===================================================================

bool MountainAgent::randomPoint(Point& point, int minAltitude)
{
	Point p;

	if (minAltitude == 0)
	{
		peaks.random_member(point);
		return true;
	}

	int pos = rand() % peaks.size();
	if (! peaks.position(pos))
	{
		Logger::Instance().Log ("unable to position mountain iterator at %d\n", pos);
		exit (1);
	}

	while (peaks.Iterate_Next(p))
	{
		int height = Executive::Instance().getHeight(p);
		if (height > minAltitude)
		{
			point = p;
			return true;
		}
	}

	return false;
}

bool MountainAgent::randomBase (Point& base, Point& center)
{
	PointPair p;
	vector<PointPair>::iterator i = basePoints.begin();

	int pos = rand() % basePoints.size();
	std::advance(i, pos);

	base = i->base;
	center = i->center;
	return true;
}
