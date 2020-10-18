FROM node:alpine as build-env
WORKDIR /app

COPY package*.json /app/
RUN npm install
COPY . ./

RUN mkdir /app/dist

FROM nginx:alpine
COPY --from=build-env /app/dist /usr/share/nginx.html
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/nginx.conf
COPY nginx/nginx.conf /etc/nginx/conf.d
COPY nginx/Certificates /etc/nginx/Certificates/
EXPOSE 80 443
ENTRYPOINT ["nginx", "-g", "daemon off;"]
