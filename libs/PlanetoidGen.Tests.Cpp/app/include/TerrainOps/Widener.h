#ifndef WIDENER_H
#define WIDENER_H

#include "TerrainOp.h"
#include "point.h"

class WidenerOp : public TerrainOp
{
private:
	int width;
	int direction;
	Point previous;
	int prev_dir;
	bool smooth;
	int decay_elev;

	TerrainOp& subOp;

	// these are the endpoints of the previous slice, in case someone wants to know the boundaries
	Point leftTail;
	Point rightTail;

//	void elevateSlice (Point& p);

	void processLocation (Point& location, int dir);
	void convertDirection (int dir, int& dx, int& dy);
	bool isDiagonal (int dir);
	void step (int& x, int& y, int dir);
	void changeDirection (int x, int y, int dir);
	void processSlice (int x, int y, int dir);

public:
	WidenerOp (TerrainOp& op, int w, int dir, bool doSmooth);
	virtual void Execute (Point& p);
	void setPrevious (Point& p, int dir);
	void setDecay (int v)		{ decay_elev = v; }
	void getTailPoints (Point& left, Point& right);
};
#endif