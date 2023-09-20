#ifndef WATER_MODEL
#define WATER_MODEL

#include "point.h"
#include "WaterNode.h"
#include <vector>
#include <memory>
#include "Lake.h"

typedef std::vector<std::vector<WaterNode> > WaterMap;
typedef std::map<int,int> LakeMap;			// map point id to lake index

class WaterModel
{
private:
	static std::unique_ptr<WaterModel> _instance;
	WaterMap map;
	std::vector<Lake> lakes;
	PointSet visited;

	// comparators
	bool flowCompare (WaterNode& left, WaterNode& right);
	bool heightCompare (WaterNode& left, WaterNode& right);

	void loadVector (std::vector<WaterNode>& dest);
	bool findLowestNotInPath (Point& current, Point& next);
	bool highestInflowNeighbor(int lake_id, Point& src, Point& neighbor);
	int startLake (Point& p, Point& inflow);
	bool downstreamPoint (Point& src, Point& downstream);

	void setLakeDownstream (int lake_id, Point& down);
	void setLakeUpstream (int lake_id, Point& up);
	void propagateDownStream (Point& p, int flow);
	void checkLakesForPoint (Point& p);
protected:
	WaterModel ();

public:
	static WaterModel& Instance();
	bool lookupNode (Point& p, WaterNode& n);
	void setFlowVectors ();
	void printAllFlows ();
	int flowAt (Point& p);
};

#endif