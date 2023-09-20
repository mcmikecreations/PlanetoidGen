#ifndef EXEC_H
#define EXEC_H

#include <memory>
#include "map.h"
#include "heightmap.h"
#include <set>
#include "point.h"
#include "pointset.h"
#include "index.h"
#include <string>
#include "TerrainOp.h"

class Agent;
class MountainAgent;

typedef std::set<Agent *> AgentSet;
enum {TEXTURE_DIRT1, TEXTURE_DIRT2, TEXTURE_DIRT3, TEXTURE_GRASS1, TEXTURE_GRASS2,
			TEXTURE_ROCK1, TEXTURE_ROCK2, TEXTURE_ROCK3, TEXTURE_ROCK4, TEXTURE_ROCK5,
			TEXTURE_SAND, TEXTURE_ICEROCK1, TEXTURE_ICEROCK2, TEXTURE_ICE, TEXTURE_LAVA, TEXTURE_SNOW};

typedef enum {GRAD_MAX_SLOPE, GRAD_MIN_SLOPE, GRAD_MAXIMUM, GRAD_MINIMUM} GradientType;

class Executive
{
private:
	static std::unique_ptr<Executive> _instance;
	Map *mask;
	Heightmap *map;
	Index *texture;

	PointSet fixed_points;
	PointSet coastline;
	PointSet watched;
	PointSet ocean;

	int atlas_size;					// number of textures stored in the atlas

	AgentSet runnable;
	AgentSet mountainAgents;
	AgentSet riverAgents;

	int numMountainAgents;

	void generate_plsm_cfg ();
	unsigned long weightedAverageHeight (Point& p);

	int indexOf (int slice);
	int maxGradient (Point& p);

	void identifyCoastline();
	int oppositeDirection (int direction);

	void shock_map (int num_points);
	void randomBlob (int x, int y, int height);

protected:
	Executive ();

public:
	static Executive& Instance();
	std::string currentTime();
	bool random_neighbor (Point& src, Point& neighbor);
	bool random_land (Point& point);
	bool random_boundary (Point& point, int maxAltitude = 0);
	bool random_mountain (Point& point, int minAltitude = 0);
	MountainAgent *randomMountainAgent ();

	bool neighbor (Point& seed, Point& neighbor, int direction);
	bool on_land (Point& point);
	bool on_shore (Point& point);
	inline bool inOcean (Point& point)		{ return ocean.in_set(point);}

	bool StepDir (Point& src, Point& dst, int direction, int delta = 1);
	int  directionFrom (Point& src, Point& target);

	bool findInterior (Point& point, int distance);
	int distanceSq (Point& p1, Point& p2);
	int distanceToCoastline (Point& p);

	unsigned long maxAltitude ();
	unsigned long minAltitude ();

	unsigned long getHeight (Point& p);
	void setHeight (Point& point, unsigned long alt);
	void smoothPoint (Point& point);					// originally in SmoothAgent, but needed elsewhere too
	void smoothArea (Point& point);
	void assignArea (Point& point, int altitude);
	inline void setMask (Map *map)			{mask = map;}
	void addAgent (Agent *a);
	void agentCompleted (Agent *a);
	bool anyRunnable (AgentSet& agents);
	void makeRunnable (AgentSet& agents);

	inline bool onMap(Point& p)				{return mask->in_range(p.x, p.y);}
	inline void fixPoint(Point& p)			{fixed_points.insert(p);}
	bool onMaskBoundary (Point& p);

	void fixArea (Point& p);
	inline bool isFixed(Point& p)			{return fixed_points.in_set(p); }
	inline void unfix(Point& p)				{fixed_points.remove(p.x, p.y);}
	void operatePoint (Point& p, TerrainOp& op);
	void operateArea (Point& p, TerrainOp& op);
	// void randomWalk (Point& p, TerrainOp& op);			// a possibility for the future
	void findGradient (GradientType, Point& center, Point& p, int& slope);

	inline void watchPoint(Point& p)		{watched.insert(p);}
	inline bool isWatched(Point& p)			{return watched.in_set(p); }

	void texturePoint (Point& p, int texture_id);
	void textureArea (Point& point, int texture_id);
	void printArea (Point& p);

	void writeHeightmap ();
	void Setup ();
	void PostRun ();
	void Run();
};

#endif