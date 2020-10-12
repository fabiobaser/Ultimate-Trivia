import React, { Component } from "react";
import { Card, Grid, Label, List, Feed, Button } from "semantic-ui-react";
import SelectButton from "./SelectButton";

export default class Game extends Component {
  render() {
    const {
      handleAnswerSelect,
      possibleAnswers,
      userArray,
      points,
      name,
      chat,
      createGame,
      leaveLobby,
      topics,
      handleTopicSelect,
      question,
      inGame,
    } = this.props;

    return (
      <Grid celled="internally">
        <Grid.Row>
          <Grid.Column width={3}>
            {userArray.length > 0 && (
              <Card>
                <Card.Content>
                  <Card.Header>Spieler</Card.Header>
                  <List>
                    {userArray.map((user) => (
                      <List.Item key={user}>
                        <Label className="fluid" basic={name === user}>
                          {user}
                          <Label.Detail style={{ float: "right" }}>
                            {(points || {})[user] || 0}
                          </Label.Detail>
                        </Label>
                      </List.Item>
                    ))}
                  </List>
                </Card.Content>
              </Card>
            )}
          </Grid.Column>
          <Grid.Column width={10}>
            {topics.length > 0 && (
              <div>
                <h2>Wähle eine Frage aus</h2>
                {topics.map((t) => (
                  <SelectButton key={t} value={t} handler={handleTopicSelect} />
                ))}
              </div>
            )}

            {possibleAnswers.length > 0 && (
              <div>
                <h2>{question}</h2>
                {possibleAnswers.map((a) => (
                  <SelectButton
                    key={a}
                    value={a}
                    handler={handleAnswerSelect}
                  />
                ))}
              </div>
            )}
          </Grid.Column>
          <Grid.Column width={3}>
            <Button.Group>
              {!inGame && (
                <Button basic color={"green"} onClick={createGame}>
                  Start
                </Button>
              )}
              <Button basic color={"red"} onClick={leaveLobby}>
                Spiel verlassen
              </Button>
            </Button.Group>

            <Feed>
              {chat.map((post, postIndex) => {
                return (
                  <Feed.Event key={postIndex}>
                    <Feed.Content>
                      <Feed.Date>{post.sender}</Feed.Date>
                      <Feed.Summary>{post.message}</Feed.Summary>
                    </Feed.Content>
                  </Feed.Event>
                );
              })}
            </Feed>
          </Grid.Column>
        </Grid.Row>
      </Grid>
    );
  }
}
