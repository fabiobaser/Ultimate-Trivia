FROM node:alpine as build-env
WORKDIR /app

RUN chmod +x docker-entrypoint.sh

COPY package*.json /app/
RUN npm install
COPY . ./
RUN npm run build

FROM nginx:alpine
COPY --from=build-env /app/dist /usr/share/nginx.html
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx/nginx.conf /etc/nginx/conf.d
COPY nginx/Certificates /etc/nginx/Certificates/
EXPOSE 80 443
CMD ["nginx", "-g", "daemon off;"]
