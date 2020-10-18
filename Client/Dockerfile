FROM node:alpine as build-env
WORKDIR /app

COPY package*.json /app/
RUN npm install
COPY . ./

RUN mkdir /app/dist

FROM nginx:alpine
COPY --from=build-env /app/dist /usr/share/nginx.html
COPY nginx/Certificates /etc/nginx/Certificates/
EXPOSE 80 443
CMD nginx -g "daemon off;"
