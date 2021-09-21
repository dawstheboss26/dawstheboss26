#!/usr/bin/env python3
# parsing.py
# Created by Dawson Flatley to work with Madeline Herbert's "gui.py" and Raul Amezcua's DLList sorting
# Part of team NovoDynamics project 
import sys
import os
import json
import pathlib
from pathlib import Path

# findtags
# Inputs: 
#	folder: path of folder with all VoTT .json files
#	tags: list of tags to look for (photo must include all specified tags)
# Outputs:
#	outlist: a dictionary in which the number of regions as a key and filename of photo as the value
def findtags(folder,tags):
	outlist = {} # Output dictionary
	filteredtags = {} # Dictionary of booleans to indicate which tags are found
	for filename in os.listdir(folder): #iterate through directory of jsons
		for tag in tags: # Set each tags "found" bool to false
			filteredtags[tag] = False;
		mypath = folder + "/" + filename #build full path to the .json file
		with open(mypath) as f:
			data = json.load(f)
			for region in data["regions"]:
				for intag in region["tags"]:
					if intag in filteredtags: # Check if tag is one of user-specified tags
						filteredtags[intag] = True
		if all(filteredtags.values()): # Needs to have found all tags
			outlist[len(data["regions"])] = data["asset"]["name"] #Key: number of regions, Value: name of image file
	return outlist
			
if __name__ == "__main__":
	if len(sys.argv) < 3 or len(sys.argv) >3:
		print("Usage:    [program] [json directory] [list of tags to search for]")
		exit()
	findtags(sys.argv[1], sys.argv[2])
		
			
			
			
		
