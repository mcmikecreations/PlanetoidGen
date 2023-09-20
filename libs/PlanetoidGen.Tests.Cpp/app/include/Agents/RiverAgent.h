#ifndef RIVERAGENT_H
#define RIVERAGENT_H

#include "agent.h"
#include "point.h"
#include "pointset.h"
#include <vector>

typedef std::vector<Point> PointList;

class RiverAgent : public Agent
{
public:
	RiverAgent (int _tokens);
	bool Execute ();

private:
	Point location;
	static int count;
	bool initializing;
	int width;
	int prev_height;

	// the previous step's position and orientation, so we can pick up where they left off
	Point previous;
	int prev_dir;

	Point startPoint;
	Point endPoint;
	bool findingPath;

	PointSet points;				// the set of all points in the river (not just centerline)
	PointList path;					// centerline of new river
	PointList mergePoints;

	int base_direction;				// the primary direction, we can zig-zag about this, but must stay within 90deg
	int current_direction;

	void calculatePath (Point& startPoint, Point& endPoint, PointList& path);
	void buildRiverSegment (PointList& path);

	void advancePath ();				// move the agent forward
	void buildRiver(Point& location, int width);	// widen, texture and fix a point on the river

	void changeDirection();
	int bestDirection(Point& p);

	void saveMergePoints (PointList& path);
	void finishRiver ();
	void buildLake (Point& p, int height);

	bool findSuitableShorePoint (Point& p);
	bool findSuitableMountainPoint (Point& p);
	bool surroundingPtsInSet (Point& p, PointSet& pointset);

};
#endif