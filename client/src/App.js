import React, { Component } from "react";
import { Input, Image, Container, Header, Modal } from "semantic-ui-react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import faker from "faker";
import JoinCreateGame from "./Components/JoinCreateGame";
import Game from "./Components/Game";

export default class App extends Component {
  constructor(props) {
    super(props);

    this.state = {
      name: faker.name.firstName(),
      lobbyId: "",
      userArray: [],
      connectedToLobby: false,
      showSelectableQuestions: false,
      selectableQuestions: [],
      possibleAnswers: [],
      nameModalOpen: true,
      chat: [],
      inGame: false,
      topics: [],
      question: "",
    };
  }

  componentDidMount() {
    this.connectToHub();
  }

  createGame = () => {
    this.connection.invoke("CreateGame", { rounds: 3, roundDuration: 30 });
  };

  createLobby = () => {
    this.connection.invoke("CreateLobby", this.state.name);
  };

  joinLobby = () => {
    const { name, lobbyId } = this.state;
    console.log("%cConnecting to code: ", "color: orange", lobbyId);
    this.connection.invoke("JoinLobby", name, lobbyId);
  };

  leaveLobby = () => {
    this.pushtToChat("", "You left the lobby");
    this.connection.invoke("LeaveLobby");
  };

  pushtToChat = (sender, message) => {
    const newEntry = { sender, message };
    const chat = this.state.chat;
    chat.push(newEntry);

    this.setState({ chat: chat });
  };

  connectToHub = () => {
    this.connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5000/triviaGameServer")
      .build();

    this.connection.on("broadcastMessage", (username, message) => {
      console.log(username, message);
    });

    this.connection.on("joinLobby", (joinLobbyEvent) => {
      this.pushtToChat("", "Du bist dem Spiel beigetreten");
      this.setState({
        userArray: joinLobbyEvent.usernames,
        connectedToLobby: true,
        lobbyId: joinLobbyEvent.lobbyId,
        inGame: false,
      });
    });

    this.connection.on("userJoinedLobby", (userJoinedEvent) => {
      console.log(`${userJoinedEvent.newUser} joined the lobby`);
      console.log("In the lobby are: ", userJoinedEvent.usernames);
      this.setState({ userArray: userJoinedEvent.usernames });
    });

    this.connection.on("leaveLobby", () => {
      console.log(`You have left the lobby`);
      this.setState({ connectedToLobby: false, userArray: [] });
    });

    this.connection.on("userLeftLobby", (userLeftEvent) => {
      console.log(`${userLeftEvent.leavingUser} has left the lobby`);
      console.log("In the lobby are: ", userLeftEvent.usernames);

      this.setState({ userArray: userLeftEvent.usernames });
    });

    this.connection.on("gameStarted", () => {
      this.setState({ inGame: true });
    });

    this.connection.on("showCategories", (showCategoriesEvent) => {
      console.log(
        `user ${showCategoriesEvent.username} is choosing a category`
      );

      this.pushtToChat(
        "",
        `${showCategoriesEvent.username} wählt ein Thema aus`
      );

      if (showCategoriesEvent.username === this.state.name) {
        this.setState({ topics: showCategoriesEvent.categories });
      }
    });

    this.connection.on("showQuestion", (showQuestionEvent) => {
      this.setState({
        possibleAnswers: showQuestionEvent.answers,
        question: showQuestionEvent.question,
      });
    });
    
    // TODO: handle correctly
    this.connection.on("userAnswered", (userAnsweredEvent) => {
      console.log(`${userAnsweredEvent.username} has answered`);
    });

    // TODO: handle correctly
    this.connection.on("highlightCorrectAnswer", (highlightCorrectAnswerEvent) => {
      console.log(`correct answer was ${highlightCorrectAnswerEvent.correctAnswer}`);
    });
    
    this.connection.on("updatePoints", (updatePointsEvent) => {
      this.setState({ points: updatePointsEvent.points });
    });

    this.connection.on("showFinalResult", (showFinalResultEvent) => {
      console.log("Final result: ", showFinalResultEvent.points);
      this.pushtToChat("", "Spiel zuende");
    });

    // TODO: handle correctly
    this.connection.on("error", (errorEvent) => {
      
      if(errorEvent.errorCode === "DUPLICATE_USERNAME"){
        console.error("username already taken")
        console.error(errorEvent.errorMessage)
      } else {
        console.error(errorEvent.errorCode + ' ' + errorEvent.errorMessage)
      }
    });

    this.connection
      .start()
      .then(
        (resolve) => {
          console.log(resolve);
        },
        (reject) => {
          console.log(reject);
        }
      )
      .catch((error) => {
        console.log(error);
      });
  };

  handleInputChange = (e, props) => {
    const { name, value } = props;
    if (this.state.connectedToLobby) return;
    const newState = this.state;
    newState[name] = value;
    this.setState(newState);
  };

  handleAnswerSelect = (answer) => {
    this.connection.invoke("AnswerSelected", answer);
    this.setState({ possibleAnswers: [] });
  };

  handleTopicSelect = (topic) => {
    this.connection.invoke("CategorySelected", topic);
    this.setState({ topics: [] });
  };

  closeModal = () => {
    this.setState({ nameModalOpen: false });
  };

  render() {
    const {
      connectedToLobby,
      lobbyId,
      name,
      chat,
      topics,
      question,
      inGame,
    } = this.state;

    return (
      <Container>
        <Header as="h1">Wilkommen bei Ultimate Trivia</Header>

        {!connectedToLobby && (
          <JoinCreateGame
            lobbyId={lobbyId}
            joinLobby={this.joinLobby}
            leaveLobby={this.leaveLobby}
            createLobby={this.createLobby}
            connectedToLobby={connectedToLobby}
            handleInputChange={this.handleInputChange}
          />
        )}

        {connectedToLobby && (
          <Game
            handleAnswerSelect={this.handleAnswerSelect}
            possibleAnswers={this.state.possibleAnswers}
            userArray={this.state.userArray}
            points={this.state.points}
            createGame={this.createGame}
            leaveLobby={this.leaveLobby}
            name={name}
            chat={chat}
            topics={topics}
            question={question}
            inGame={inGame}
            handleTopicSelect={this.handleTopicSelect}
          />
        )}

        <h1 style={{ width: "100%", textAlign: "center" }}>{lobbyId}</h1>

        <Modal size={"mini"} open={this.state.nameModalOpen}>
          <Modal.Header style={{ paddingTop: "2rem" }}>
            Gib einen Namen ein{" "}
            <Image
              style={{ float: "right" }}
              src={`https://avatar.tobi.sh/${name}.svg?text=${name
                .slice(0, 2)
                .toUpperCase()}`}
              avatar
            />
          </Modal.Header>
          <Modal.Content>
            <p>Zum Spielen brauchst du einen Namen</p>

            <Input
              fluid
              name={"name"}
              action={{ icon: "rocket", onClick: this.closeModal }}
              value={this.state.name}
              onChange={this.handleInputChange}
            />
          </Modal.Content>
        </Modal>
      </Container>
    );
  }
}
