#ifndef EROSION_AGENT_H
#define EROSION_AGENT_H

#include "agent.h"
#include <vector>
#include <set>
#include "point.h"

// An ErosionAgent builds a spanning tree from a heightmap based on terrain gradients.  Each point on the map
// will direct water flow to one neighboring point.  This means that each point has a certain capacity, an intrinsic
// inflow (due to rain), and an outflow derived from points which feed into this.

class TreeNode
{
public:
	TreeNode ();
	inline void setGradient(int grad)				{ gradient = grad; }
	inline int getGradient ()						{ return gradient; }
	inline void addInflow (int flow)				{ inflow += flow; }
	inline int getOutflow ()						{ return inflow; }
	inline void unsetRoot()							{ treeRoot = false; }
	inline void setID(int i)						{ id = i; }
	inline int getID()								{ return id; }
	int getHeight();
	Point upstream;
	Point downstream;
	int direction_into;								// direction of flow into this node
	int direction_out;
	int largestFlow;								// largest single flow into this node (determines direction)


private:
	int id;
	int gradient;
	int inflow;

	bool treeRoot;

	void pointOf(int id, Point& p);
};

bool operator < (TreeNode& left, TreeNode& right);

class ErosionAgent : public Agent
{
public:
	ErosionAgent ();
	bool Execute ();

	void addNode (Point& p, TreeNode& node);

private:
	std::vector<TreeNode> forest;
	std::vector<TreeNode> sortedForest;
	std::vector<TreeNode>::iterator forest_iterator;
	int flowPosition;						// current node when flowing water out of a point
	int currentRoot;
	std::set<int> currentPath;				// nodes visited in this walk

	int current_x;
	int current_y;

	int indexOf (int x, int y);
	inline int indexOf (Point& p)			{ return indexOf (p.x, p.y); }
	void pointOf (int id, Point& p);
	int flowAt(Point& p);

	int startLake (Point& p, Point& downstream);
	void buildSpanningForests();
	int waterFlow(int id);
	void flowWater();
	void textureRiver (Point& p, int width);
	bool inPath (Point& p);
	bool inPath (int id);
	bool downstreamPoint (Point& src, Point& downstream);
	bool highestInflowNeighbor (int lake_id, Point& src, Point& neighbor);

	void setDownstream (Point& p, Point& down);
	void setUpstream (Point& p, Point& up);
	void setLakeDownstream (int lake_id, Point& down);
	void setLakeUpstream (int lake_id, Point& up);

	void printHeightmap ();
	void adjustFlow (Point& p, int flow);			// propagate flow downstream

	bool findLowestNotInPath (Point& current, Point& next);
	void performTextureWalk ();
	bool buildingForests;

	Point previous;
	int prev_height;
	int current_direction;
	int prev_dir;
};

#endif