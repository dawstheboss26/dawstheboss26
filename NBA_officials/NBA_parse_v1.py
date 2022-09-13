#!user/bin/env python3

import re
import requests
import json
import os

beginning = 21300000
end = 30000000
try:
        f = open("lastcompleted.txt", "r")
        saved = int(f.read())
        f.close()
        if saved > beginning:
                beginning = saved+1
        tryurl("https://www.nba.com/game/"+str(beginning))
except:
        pass
def tryurl(url):
                r = requests.get(url)
                print(r.text)
                find = re.compile(r'"application/json">(.*?)</script></body></html>')

                here = re.findall(find, r.text)


# here is now a json string

                game_official_dict = json.loads(here[0])
                print ("JSON LOADED")

                gameID = game_official_dict["props"]["pageProps"]["game"]["gameId"]
                gameDate = game_official_dict["props"]["pageProps"]["game"]["gameEt"][0:9]
                gameLength = game_official_dict["props"]["pageProps"]["game"]["duration"]

                string1Append = gameID+','+gameDate+','+gameLength+'\n'
                print(string1Append)
                f = open("moreData.csv", "a")
                f.write(string1Append)
                f.close()

                for player in game_official_dict["props"]["pageProps"]["game"]["homeTeam"]["players"]:
                        string2Append = gameID+","+player["firstName"]+" "+player["familyName"]
                        for stat in player["statistics"]:
                                string2Append = string2Append+","+str(player["statistics"][stat])
                        string2Append = string2Append+"\n"
                        f = open("hugePlayerData.csv", "a")
                        f.write(string2Append)
                        f.close()
                for player in game_official_dict["props"]["pageProps"]["game"]["awayTeam"]["players"]:
                        string2Append = gameID+","+player["firstName"]+" "+player["familyName"]
                        for stat in player["statistics"]:
                                string2Append = string2Append+","+str(player["statistics"][stat])
                        string2Append = string2Append+"\n"
                        f = open("hugePlayerData.csv", "a")
                        f.write(string2Append)
                        f.close()



for i in range(beginning, end):
        if i % 10000 >= 2000:
                continue
        offset = i + 20000000
        url1 = 'https://www.nba.com/game/00'+str(i)
        url2 = 'https://www.nba.com/game/00'+str(offset)
        try:
                tryurl(url1)
        except:
                print(url1+" failed\n")
        try:
                tryurl(url2)
        except:
                print(url2+" failed\n")
        os.remove("lastcompleted.txt")
        f = open("lastcompleted.txt", "w")
        f.write(str(i))
        f.close()
