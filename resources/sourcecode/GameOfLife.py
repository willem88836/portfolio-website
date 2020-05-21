import copy
import os 
import random
import sys
import threading
import time

class GameOfLife (threading.Thread):
	display = None
	time = 0

	# Prints the current display. 
	def print(self):
		os.system('cls')
		i = 0
		while i < len(self.display):
			row = self.display[i]
			j = 0
			while j < len(row):
				if self.display[i][j] > 0:
					print("#", end='')
				else: 
					print(" ", end='')
				j += 1
			print("")
			i += 1

	# Iterates through the display t times.
	def run(self):
		t = 0
		while t < self.time:
			displayCopy = copy.deepcopy(self.display)
			self.print()
			print(t - self.time)
			time.sleep(1)
			# Iterates through all he different cells. 
			y = 0
			while y < len(self.display): 
				x = 0
				while x < len(self.display[y]):
					n = 0
					i = y - 1
					while i < y + 2:
						# Iterates through all the cell's neighbours. 
						j = x - 1
						while j < x + 2:
							# Only if it is in-bounds and not the current cell.
							if ((i >= 0) and 
									(j >= 0) and 
									(i < len(self.display)) and 
									(j < len(self.display[i]) and 
									((i != y) and (j != x)))):
								if self.display[i][j] > 0:
									n += 1
							j += 1
						# If there are exactly enough neighbours, the cell becomes alive.
						# If there are too few or many, it dies. 
						# Else, it stays whatever it is. 
						s = self.display[y][x]
						if n < 2 or n > 3: 
							s = 0
						elif n == 3: 
							s = 1
						displayCopy[y][x] = s
						i += 1
					x += 1
				y += 1
			self.display = displayCopy
			t += 1

	# Initializes Conway's Game of Life.
	def __init__(self, resX, resY, t):
		threading.Thread.__init__(self)
		self.display = []
		self.time = t
		i = 0
		while i < resY: 
			row = []
			self.display.append(row)
			j = 0
			while j < resX:
				row.append(random.randint(0, 1))
				j += 1
			i += 1
		self.start()

gol = GameOfLife(int(sys.argv[1]), int(sys.argv[2]), int(sys.argv[3]))
