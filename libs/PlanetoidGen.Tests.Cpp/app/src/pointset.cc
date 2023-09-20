#include <algorithm> 
#include "pointset.h"
#include "logger.h"
#include "params.h"

PointSet::PointSet ()
{
	Params& params = Params::Instance();

	impl_iter = points.begin();
	x_dim = params.x_size;
	y_dim = params.y_size;
}

PointSet::PointSet(int x_size, int y_size)
{
	impl_iter = points.begin();
	x_dim = x_size;
	y_dim = y_size;
}

PointSet::~PointSet ()
{
}


// ==========================================================
// in_set:  determine if a point is in the set
// ==========================================================
bool PointSet::in_set (int x, int y)
{
	int coord = y * x_dim + x;

	if (points.find(coord) != points.end())
	{
		return true;
	}
	else
	{
		return false;
	}


	return false;
}

// ==========================================================
// in_set:  determine if a point is in the set
// ==========================================================
bool PointSet::in_set (Point& p)
{
	return in_set (p.x, p.y);
}

// ==========================================================
// random_member -- return a random point in the set
//
// New storage is allocated and ownership transferred to the caller,
// the internal set ownership is retained.
// ==========================================================
bool PointSet::random_member (Point& point)
{
	if (points.size() == 0)
		return false;

	int pos = rand() % points.size();

	IntegerSet::iterator i = points.begin();
	std::advance(i, pos);

	int coord = *i;
	point.x = coord % x_dim;
	point.y = coord / x_dim;

	return true;
}

// ==========================================================
// random_member -- return a random point in the set
//
// New storage is allocated and ownership transferred to the caller,
// the internal set ownership is retained.
// ==========================================================
bool PointSet::member (int pos, Point& point)
{
	if (pos >= (int) points.size())
		return false;

	IntegerSet::iterator i = points.begin();
	std::advance(i, pos);

	int coord = *i;
	point.x = coord % x_dim;
	point.y = coord / x_dim;

	return true;
}

bool PointSet::position(int pos)
{
	IntegerSet::iterator i = points.begin();
	std::advance(i, pos);

	if (i != points.end())
	{
		impl_iter = i;
		return true;
	}
	else
	{
		return false;
	}
}

// ==========================================================
// insert -- insert a point into the set
// ==========================================================
void PointSet::insert (int x, int y)
{
	if (! in_set (x, y))
	{
		int coord = y * x_dim + x;
		points.insert (coord);
	}
}

// ==========================================================
// insert -- insert a point into the set
// ==========================================================
void PointSet::insert (Point& p)
{
	insert (p.x, p.y);
}

// ==========================================================
// remove -- remove a point from the set
// ==========================================================
void PointSet::remove (int x, int y)
{
	int coord = y * x_dim + x;

	if (points.find(coord) != points.end())
	{
		points.erase (coord);
	}
}

void PointSet::Reset_Iterator ()
{
	impl_iter = points.begin ();
}

bool PointSet::Iterate_Next (Point& next)
{
	if (impl_iter == points.end())
	{
		return false;
	}

	if (x_dim == 0)
	{
		return false;
	}

	int coord = *impl_iter;

	next.x = coord % x_dim;
	next.y = coord / x_dim;

	++impl_iter;
	return true;
}

bool PointSet::is_empty ()
{
	return (points.size() == 0);
}

void PointSet::printSet ()
{
	IntegerSet::iterator iter;

	for (iter = points.begin(); iter != points.end(); ++iter)
	{
		Point p;
		pointOf (*iter, p);

		Logger::Instance().Log (" (%d,%d) ", p.x, p.y);
	}

	Logger::Instance().Log ("\n");
}

void PointSet::pointOf(int id, Point& p)
{
	Params& params = Params::Instance();

	p.x = id % params.x_size;
	p.y = id / params.x_size;
}