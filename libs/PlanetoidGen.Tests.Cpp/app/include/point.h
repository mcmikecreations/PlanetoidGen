#ifndef POINT_H
#define POINT_H

#include <set>
#include <list>
#include <map>

typedef unsigned int CoordType;

class Point
{
public:
	CoordType x;
	CoordType y;
	bool set;

	bool operator== (const Point& right) const;
	bool operator!= (const Point& right) const;
	bool operator< (const Point& right) const;
	Point ();
	Point (int x, int y);
};

#if 0
// ==========================================================
// STL Point Containers
// ==========================================================

struct point_compare
{
	bool operator() (const Point *lhs, const Point *rhs) const
	{
		return *lhs < *rhs;
	}
};


//typedef std::set<Point *, point_compare>	PointSet;
//typedef PointSet::const_iterator 			PointIter;
typedef std::list<Point *> 					PointList;
typedef PointList::const_iterator 			PointListIter;
typedef std::map<Point *,long> 				PointMap;

// functor to assist with clearing containers
struct ptr_delete 
{
	template< class T >
	void operator()( T * const p ) const 
	{
		delete p;
	}
};
#endif

#endif
