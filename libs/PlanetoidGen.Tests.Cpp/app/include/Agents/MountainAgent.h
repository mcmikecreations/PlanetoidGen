#ifndef MOUNTAIN_AGENT_H

#include "agent.h"
#include "point.h"
#include "heightmap.h"
#include <vector>

typedef struct
{
	Point base;
	Point center;
} PointPair;

class MountainAgent : public Agent
{
public:
	MountainAgent (int _tokens);

	bool Execute ();

	bool randomPoint (Point& p, int minAltitude = 0);
	bool randomBase (Point& base, Point& center);

	inline void setAltitudePreferences (int alt, int var)		{ altitude = alt; variance = var; }
	inline void setDirection(int dir)							{ base_direction = dir; current_direction = dir; prev_dir = dir; }
	inline void setLocation (Point loc)							{ location = loc; }
	inline void setWidth(int w)									{ width = w; }

private:
	void changeDirection ();
	void elevateSlice (Point& p, int altitude, int width);

	Point location;
	static int count;

	// the previous step's position and orientation, so we can pick up where they left off
	Point previous;
	int prev_dir;

	int base_direction;
	int current_direction;

	int altitude;		// my preferred altitude
	int variance;		// possible deviation from preferred
	int width;

	PointSet peaks;
	// PointSet basePoints;
	std::vector<PointPair> basePoints;
	bool initializing;
};

#endif