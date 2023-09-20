#ifndef POINTSET_H
#define POINTSET_H

#include "point.h"
#include <set>

typedef std::set<int> IntegerSet;

class PointSet
{
private:
	IntegerSet points;
	IntegerSet::iterator impl_iter;

	int x_dim;
	int y_dim;
	void pointOf(int id, Point& p);

public:
	PointSet ();
	PointSet (int x_dim, int y_dim);
	virtual ~PointSet();

	inline void setSize (int x_size, int y_size)	{x_dim = x_size; y_dim = y_size; }

	bool in_set (int x, int y);
	bool in_set (Point& p);
	bool is_empty ();

	bool random_member (Point& member);
	bool member(int index, Point& p);
	bool position(int pos);

	void insert (int x, int y);
	void insert (Point& p);
	void remove (int x, int y);
	inline void remove (Point& p)				{ remove(p.x, p.y); }
	inline int size () {return points.size();}
	void printSet ();
	void Reset_Iterator ();
	bool Iterate_Next (Point& next);
};

// note: use command pattern to implement functor tests
#endif
