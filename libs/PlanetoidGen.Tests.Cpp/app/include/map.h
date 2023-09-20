#ifndef MAP_H
#define MAP_H

#include "image.h"
#include "point.h"
#include "pointset.h"

class Map : public Image
{
private:
	int coverage;
	int num_points;
	PointSet boundary;
	int generated;

	void Peephole (int level);
	bool is_surrounded (uint x, uint y, int level);
	void Smooth (int walksize);
	void smooth_walk (uint x, uint y, int steps);
	void smooth_cell (uint x, uint y);
	void smooth_plus (uint x, uint y);
	bool neighbor (Point& seed, Point& neighbor, int direction);

public:
	Map (uint x_size, uint y_size);
	Map (std::string);
	Map ();
	~Map ();

	void Clear ();

	void generate_mask ();

	// setters
	inline void Set_NumPoints (int v) {num_points = v;}
	inline void Set_Coverage (int c) {coverage = c;}
	bool on_boundary (int x, int y);
	void Set (uint x, uint y, unsigned long value);
	bool is_set (uint x, uint y);
	bool is_surrounded (Point& p);
	int num_free_points (Point& p);
	void removeFromBoundary (Point& p);
	bool randomBoundaryPoint (Point& p);
	bool randomPointOnMask (Point& p);

	inline void resetBoundaryIterator ()		{ boundary.Reset_Iterator();}
	bool nextBoundaryPoint (Point &p);

	inline int getGeneratedCount()		{return generated;}
};

#endif
