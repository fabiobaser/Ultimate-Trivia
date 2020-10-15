docker stop ultimate-trivia
docker rm ultimate-trivia
docker run -d -p 5000:5000 -p 5001:5001 --name=ultimate-trivia ultimatetrivia