openssl req -x509 -nodes -new -sha256 -days 1024 -newkey rsa:2048 -keyout RootCA.key -out RootCA.pem -subj "/C=US/CN=Ultimate-Trivia-Root-CA"
openssl x509 -outform pem -in RootCA.pem -out RootCA.crt
openssl req -new -nodes -newkey rsa:2048 -keyout localhost.key -out localhost.csr -subj "/C=US/ST=YourState/L=YourCity/O=Example-Certificates/CN=localhost"
openssl x509 -req -sha256 -days 1024 -in localhost.csr -CA RootCA.pem -CAkey RootCA.key -CAcreateserial -out localhost.crt

openssl pkcs12 -export -out localhost.pfx -in localhost.crt -inkey localhost.key -passout pass:dFyaWbgBaKSAWwuYFR9w
